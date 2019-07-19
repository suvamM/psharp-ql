using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.PSharp;

namespace Benchmarks.Protocols
{
    internal class Chord
    {
        public static void Execute(IMachineRuntime runtime)
        {
            runtime.RegisterMonitor(typeof(LivenessMonitor));
            runtime.CreateMachine(typeof(ClusterManager));
        }

        private class Finger
        {
            public int Start;
            public int End;
            public MachineId Node;

            public Finger(int start, int end, MachineId node)
            {
                this.Start = start;
                this.End = end;
                this.Node = node;
            }
        }

        private class ClusterManager : Machine
        {
            private class CreateNewNode : Event { }
            private class TerminateNode : Event { }
            private class Local : Event { }

            int NumOfNodes;
            int NumOfIds;

            List<MachineId> ChordNodes;

            List<int> Keys;
            List<int> NodeIds;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventGotoState(typeof(Local), typeof(Waiting))]
            class Init : MachineState { }

            void InitOnEntry()
            {
                this.NumOfNodes = 5;
                this.NumOfIds = (int)Math.Pow(2, this.NumOfNodes);

                this.ChordNodes = new List<MachineId>();
                this.NodeIds = new List<int> { 0, 1, 3, 5, 7 };
                this.Keys = new List<int> {
                    1, 2, 4, 6, 9, 11,
                    13, 22, 27, 29, 33,
                    38, 45, 53, 67, 72,
                    81, 95, 101, 102, 104,
                    106, 109, 111, 113, 122,
                    127, 129, 133, 138, 145,
                    153, 167, 172, 181, 195
                };

                for (int idx = 0; idx < this.NodeIds.Count; idx++)
                {
                    this.ChordNodes.Add(this.CreateMachine(typeof(ChordNode)));
                }

                var nodeKeys = this.AssignKeysToNodes();
                for (int idx = 0; idx < this.ChordNodes.Count; idx++)
                {
                    var nodeId = this.NodeIds[idx];
                    if (nodeKeys.ContainsKey(nodeId))
                    {
                        var keys = nodeKeys[nodeId];
                        this.Send(this.ChordNodes[idx], new ChordNode.Config(nodeId, new HashSet<int>(keys),
                            new List<MachineId>(this.ChordNodes), new List<int>(this.NodeIds), this.Id));
                    }
                    else
                    {
                        this.Send(this.ChordNodes[idx], new ChordNode.Config(nodeId, new HashSet<int>(),
                            new List<MachineId>(this.ChordNodes), new List<int>(this.NodeIds), this.Id));
                    }
                }

                this.CreateMachine(typeof(Client),
                    new Client.Config(this.Id, new List<int>(this.Keys)));

                this.Raise(new Local());
            }

            [OnEventDoAction(typeof(ChordNode.FindSuccessor), nameof(ForwardFindSuccessor))]
            [OnEventDoAction(typeof(CreateNewNode), nameof(ProcessCreateNewNode))]
            [OnEventDoAction(typeof(TerminateNode), nameof(ProcessTerminateNode))]
            [OnEventDoAction(typeof(ChordNode.JoinAck), nameof(QueryStabilize))]
            class Waiting : MachineState { }

            void ForwardFindSuccessor()
            {
                this.Send(this.ChordNodes[0], this.ReceivedEvent);
            }

            void ProcessCreateNewNode()
            {
                int newId = -1;
                while ((newId < 0 || this.NodeIds.Contains(newId)) &&
                    this.NodeIds.Count < this.NumOfIds)
                {
                    for (int i = 0; i < this.NumOfIds; i++)
                    {
                        if (this.Random())
                        {
                            newId = i;
                        }
                    }
                }

                this.Assert(newId >= 0, "Cannot create a new node, no ids available.");

                var newNode = this.CreateMachine(typeof(ChordNode));

                this.NumOfNodes++;
                this.NodeIds.Add(newId);
                this.ChordNodes.Add(newNode);

                this.Send(newNode, new ChordNode.Join(newId, new List<MachineId>(this.ChordNodes),
                    new List<int>(this.NodeIds), this.NumOfIds, this.Id));
            }

            void ProcessTerminateNode()
            {
                int endId = -1;
                while ((endId < 0 || !this.NodeIds.Contains(endId)) &&
                    this.NodeIds.Count > 0)
                {
                    for (int i = 0; i < this.ChordNodes.Count; i++)
                    {
                        if (this.Random())
                        {
                            endId = i;
                        }
                    }
                }

                this.Assert(endId >= 0, "Cannot find a node to terminate.");

                var endNode = this.ChordNodes[endId];

                this.NumOfNodes--;
                this.NodeIds.Remove(endId);
                this.ChordNodes.Remove(endNode);

                this.Send(endNode, new ChordNode.Terminate());
            }

            void QueryStabilize()
            {
                foreach (var node in this.ChordNodes)
                {
                    this.Send(node, new ChordNode.Stabilize());
                }
            }

            Dictionary<int, List<int>> AssignKeysToNodes()
            {
                var nodeKeys = new Dictionary<int, List<int>>();
                for (int i = this.Keys.Count - 1; i >= 0; i--)
                {
                    bool assigned = false;
                    for (int j = 0; j < this.NodeIds.Count; j++)
                    {
                        if (this.Keys[i] <= this.NodeIds[j])
                        {
                            if (nodeKeys.ContainsKey(this.NodeIds[j]))
                            {
                                nodeKeys[this.NodeIds[j]].Add(this.Keys[i]);
                            }
                            else
                            {
                                nodeKeys.Add(this.NodeIds[j], new List<int>());
                                nodeKeys[this.NodeIds[j]].Add(this.Keys[i]);
                            }

                            assigned = true;
                            break;
                        }
                    }

                    if (!assigned)
                    {
                        if (nodeKeys.ContainsKey(this.NodeIds[0]))
                        {
                            nodeKeys[this.NodeIds[0]].Add(this.Keys[i]);
                        }
                        else
                        {
                            nodeKeys.Add(this.NodeIds[0], new List<int>());
                            nodeKeys[this.NodeIds[0]].Add(this.Keys[i]);
                        }
                    }
                }

                return nodeKeys;
            }
        }

        private class ChordNode : Machine
        {
            internal class Config : Event
            {
                public int Id;
                public HashSet<int> Keys;
                public List<MachineId> Nodes;
                public List<int> NodeIds;
                public MachineId Manager;

                public Config(int id, HashSet<int> keys, List<MachineId> nodes,
                    List<int> nodeIds, MachineId manager)
                    : base()
                {
                    this.Id = id;
                    this.Keys = keys;
                    this.Nodes = nodes;
                    this.NodeIds = nodeIds;
                    this.Manager = manager;
                }
            }

            internal class Join : Event
            {
                public int Id;
                public List<MachineId> Nodes;
                public List<int> NodeIds;
                public int NumOfIds;
                public MachineId Manager;

                public Join(int id, List<MachineId> nodes, List<int> nodeIds,
                    int numOfIds, MachineId manager)
                    : base()
                {
                    this.Id = id;
                    this.Nodes = nodes;
                    this.NodeIds = nodeIds;
                    this.NumOfIds = numOfIds;
                    this.Manager = manager;
                }
            }

            internal class FindSuccessor : Event
            {
                public MachineId Sender;
                public int Key;

                public FindSuccessor(MachineId sender, int key)
                    : base()
                {
                    this.Sender = sender;
                    this.Key = key;
                }
            }

            internal class FindSuccessorResp : Event
            {
                public MachineId Node;
                public int Key;

                public FindSuccessorResp(MachineId node, int key)
                    : base()
                {
                    this.Node = node;
                    this.Key = key;
                }
            }

            internal class FindPredecessor : Event
            {
                public MachineId Sender;

                public FindPredecessor(MachineId sender)
                    : base()
                {
                    this.Sender = sender;
                }
            }

            internal class FindPredecessorResp : Event
            {
                public MachineId Node;

                public FindPredecessorResp(MachineId node)
                    : base()
                {
                    this.Node = node;
                }
            }

            internal class QueryId : Event
            {
                public MachineId Sender;

                public QueryId(MachineId sender)
                    : base()
                {
                    this.Sender = sender;
                }
            }

            internal class QueryIdResp : Event
            {
                public int Id;

                public QueryIdResp(int id)
                    : base()
                {
                    this.Id = id;
                }
            }

            internal class AskForKeys : Event
            {
                public MachineId Node;
                public int Id;

                public AskForKeys(MachineId node, int id)
                    : base()
                {
                    this.Node = node;
                    this.Id = id;
                }
            }

            internal class AskForKeysResp : Event
            {
                public List<int> Keys;

                public AskForKeysResp(List<int> keys)
                    : base()
                {
                    this.Keys = keys;
                }
            }

            internal class NotifySuccessor : Event
            {
                public MachineId Node;

                public NotifySuccessor(MachineId node)
                    : base()
                {
                    this.Node = node;
                }
            }

            internal class JoinAck : Event { }
            internal class Stabilize : Event { }
            internal class Terminate : Event { }
            internal class Local : Event { }

            int NodeId;
            HashSet<int> Keys;
            int NumOfIds;

            Dictionary<int, Finger> FingerTable;
            MachineId Predecessor;

            MachineId Manager;

            protected override int HashedState
            {
                get
                {
                    int hash = 14689;

                    if (this.Keys != null)
                    {
                        foreach (var key in this.Keys)
                        {
                            int keyHash = 37;
                            keyHash += (keyHash * 397) + key.GetHashCode();
                            hash *= keyHash;
                        }
                    }

                    return hash;
                }
            }

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventGotoState(typeof(Local), typeof(Waiting))]
            [OnEventDoAction(typeof(Config), nameof(Configure))]
            [OnEventDoAction(typeof(Join), nameof(JoinCluster))]
            [DeferEvents(typeof(AskForKeys), typeof(FindPredecessor), typeof(FindSuccessor),
                typeof(NotifySuccessor), typeof(Stabilize), typeof(Terminate))]
            class Init : MachineState { }

            void InitOnEntry()
            {
                this.FingerTable = new Dictionary<int, Finger>();
            }

            void Configure()
            {
                this.NodeId = (this.ReceivedEvent as Config).Id;
                this.Keys = (this.ReceivedEvent as Config).Keys;
                this.Manager = (this.ReceivedEvent as Config).Manager;

                var nodes = (this.ReceivedEvent as Config).Nodes;
                var nodeIds = (this.ReceivedEvent as Config).NodeIds;

                this.NumOfIds = (int)Math.Pow(2, nodes.Count);

                for (var idx = 1; idx <= nodes.Count; idx++)
                {
                    var start = (this.NodeId + (int)Math.Pow(2, (idx - 1))) % this.NumOfIds;
                    var end = (this.NodeId + (int)Math.Pow(2, idx)) % this.NumOfIds;

                    var nodeId = this.GetSuccessorNodeId(start, nodeIds);
                    this.FingerTable.Add(start, new Finger(start, end, nodes[nodeId]));
                }

                for (var idx = 0; idx < nodeIds.Count; idx++)
                {
                    if (nodeIds[idx] == this.NodeId)
                    {
                        this.Predecessor = nodes[this.WrapSubtract(idx, 1, nodeIds.Count)];
                        break;
                    }
                }

                this.Raise(new Local());
            }

            void JoinCluster()
            {
                this.NodeId = (this.ReceivedEvent as Join).Id;
                this.Manager = (this.ReceivedEvent as Join).Manager;
                this.NumOfIds = (this.ReceivedEvent as Join).NumOfIds;

                var nodes = (this.ReceivedEvent as Join).Nodes;
                var nodeIds = (this.ReceivedEvent as Join).NodeIds;

                for (var idx = 1; idx <= nodes.Count; idx++)
                {
                    var start = (this.NodeId + (int)Math.Pow(2, (idx - 1))) % this.NumOfIds;
                    var end = (this.NodeId + (int)Math.Pow(2, idx)) % this.NumOfIds;

                    var nodeId = this.GetSuccessorNodeId(start, nodeIds);
                    this.FingerTable.Add(start, new Finger(start, end, nodes[nodeId]));
                }

                var successor = this.FingerTable[(this.NodeId + 1) % this.NumOfIds].Node;

                this.Send(this.Manager, new JoinAck());
                this.Send(successor, new NotifySuccessor(this.Id));
            }

            [OnEventDoAction(typeof(FindSuccessor), nameof(ProcessFindSuccessor))]
            [OnEventDoAction(typeof(FindSuccessorResp), nameof(ProcessFindSuccessorResp))]
            [OnEventDoAction(typeof(FindPredecessor), nameof(ProcessFindPredecessor))]
            [OnEventDoAction(typeof(FindPredecessorResp), nameof(ProcessFindPredecessorResp))]
            [OnEventDoAction(typeof(QueryId), nameof(ProcessQueryId))]
            [OnEventDoAction(typeof(AskForKeys), nameof(SendKeys))]
            [OnEventDoAction(typeof(AskForKeysResp), nameof(UpdateKeys))]
            [OnEventDoAction(typeof(NotifySuccessor), nameof(UpdatePredecessor))]
            [OnEventDoAction(typeof(Stabilize), nameof(ProcessStabilize))]
            [OnEventDoAction(typeof(Terminate), nameof(ProcessTerminate))]
            class Waiting : MachineState { }

            void ProcessFindSuccessor()
            {
                var sender = (this.ReceivedEvent as FindSuccessor).Sender;
                var key = (this.ReceivedEvent as FindSuccessor).Key;
                this.Id.Runtime.Logger.WriteLine($"<ChordLog> '{this.Id}' trying to find successor of key '{key}'");
                if (this.Keys.Contains(key))
                {
                    this.Send(sender, new FindSuccessorResp(this.Id, key));
                }
                else if (this.FingerTable.ContainsKey(key))
                {
                    this.Send(sender, new FindSuccessorResp(this.FingerTable[key].Node, key));
                }
                else if (this.NodeId.Equals(key))
                {
                    this.Send(sender, new FindSuccessorResp(
                        this.FingerTable[(this.NodeId + 1) % this.NumOfIds].Node, key));
                }
                else
                {
                    int idToAsk = -1;
                    foreach (var finger in this.FingerTable)
                    {
                        if (((finger.Value.Start > finger.Value.End) &&
                            (finger.Value.Start <= key || key < finger.Value.End)) ||
                            ((finger.Value.Start < finger.Value.End) &&
                            (finger.Value.Start <= key && key < finger.Value.End)))
                        {
                            idToAsk = finger.Key;
                        }
                    }

                    if (idToAsk < 0)
                    {
                        idToAsk = (this.NodeId + 1) % this.NumOfIds;
                    }

                    if (this.FingerTable[idToAsk].Node.Equals(this.Id))
                    {
                        foreach (var finger in this.FingerTable)
                        {
                            if (finger.Value.End == idToAsk ||
                                finger.Value.End == idToAsk - 1)
                            {
                                idToAsk = finger.Key;
                                break;
                            }
                        }

                        this.Assert(!this.FingerTable[idToAsk].Node.Equals(this.Id),
                            "Cannot locate successor of {0}.", key);
                    }

                    this.Send(this.FingerTable[idToAsk].Node, new FindSuccessor(sender, key));
                }
            }

            void ProcessFindPredecessor()
            {
                var sender = (this.ReceivedEvent as FindPredecessor).Sender;
                if (this.Predecessor != null)
                {
                    this.Send(sender, new FindPredecessorResp(this.Predecessor));
                }
            }

            void ProcessQueryId()
            {
                var sender = (this.ReceivedEvent as QueryId).Sender;
                this.Send(sender, new QueryIdResp(this.NodeId));
            }

            void SendKeys()
            {
                var sender = (this.ReceivedEvent as AskForKeys).Node;
                var senderId = (this.ReceivedEvent as AskForKeys).Id;

                this.Assert(this.Predecessor.Equals(sender), "Predecessor is corrupted.");

                List<int> keysToSend = new List<int>();
                foreach (var key in this.Keys)
                {
                    if (key <= senderId)
                    {
                        keysToSend.Add(key);
                    }
                }

                if (keysToSend.Count > 0)
                {
                    foreach (var key in keysToSend)
                    {
                        this.Keys.Remove(key);
                    }

                    this.Send(sender, new AskForKeysResp(keysToSend));
                }
            }

            void ProcessStabilize()
            {
                var successor = this.FingerTable[(this.NodeId + 1) % this.NumOfIds].Node;
                this.Send(successor, new FindPredecessor(this.Id));

                foreach (var finger in this.FingerTable)
                {
                    if (!finger.Value.Node.Equals(successor))
                    {
                        this.Send(successor, new FindSuccessor(this.Id, finger.Key));
                    }
                }
            }

            void ProcessFindSuccessorResp()
            {
                var successor = (this.ReceivedEvent as FindSuccessorResp).Node;
                var key = (this.ReceivedEvent as FindSuccessorResp).Key;

                this.Assert(this.FingerTable.ContainsKey(key),
                    "Finger table of {0} does not contain {1}.", this.NodeId, key);
                this.FingerTable[key] = new Finger(this.FingerTable[key].Start,
                    this.FingerTable[key].End, successor);
            }

            void ProcessFindPredecessorResp()
            {
                var successor = (this.ReceivedEvent as FindPredecessorResp).Node;
                if (!successor.Equals(this.Id))
                {
                    this.FingerTable[(this.NodeId + 1) % this.NumOfIds] =
                        new Finger(this.FingerTable[(this.NodeId + 1) % this.NumOfIds].Start,
                        this.FingerTable[(this.NodeId + 1) % this.NumOfIds].End,
                        successor);

                    this.Send(successor, new NotifySuccessor(this.Id));
                    this.Send(successor, new AskForKeys(this.Id, this.NodeId));
                }
            }

            void UpdatePredecessor()
            {
                var predecessor = (this.ReceivedEvent as NotifySuccessor).Node;
                if (!predecessor.Equals(this.Id))
                {
                    this.Predecessor = predecessor;
                }
            }

            void UpdateKeys()
            {
                var keys = (this.ReceivedEvent as AskForKeysResp).Keys;
                foreach (var key in keys)
                {
                    this.Keys.Add(key);
                }
            }

            void ProcessTerminate()
            {
                this.Raise(new Halt());
            }

            int GetSuccessorNodeId(int start, List<int> nodeIds)
            {
                var candidate = -1;
                foreach (var id in nodeIds.Where(v => v >= start))
                {
                    if (candidate < 0 || id < candidate)
                    {
                        candidate = id;
                    }
                }

                if (candidate < 0)
                {
                    foreach (var id in nodeIds.Where(v => v < start))
                    {
                        if (candidate < 0 || id < candidate)
                        {
                            candidate = id;
                        }
                    }
                }

                for (int idx = 0; idx < nodeIds.Count; idx++)
                {
                    if (nodeIds[idx] == candidate)
                    {
                        candidate = idx;
                        break;
                    }
                }

                return candidate;
            }

            int WrapAdd(int left, int right, int ceiling)
            {
                int result = left + right;
                if (result > ceiling)
                {
                    result = ceiling - result;
                }

                return result;
            }

            int WrapSubtract(int left, int right, int ceiling)
            {
                int result = left - right;
                if (result < 0)
                {
                    result = ceiling + result;
                }

                return result;
            }

            void EmitFingerTableAndKeys()
            {
                this.Id.Runtime.Logger.WriteLine(" ... Printing finger table of node {0}:", this.NodeId);
                foreach (var finger in this.FingerTable)
                {
                    this.Id.Runtime.Logger.WriteLine("  >> " + finger.Key + " | [" + finger.Value.Start +
                        ", " + finger.Value.End + ") | " + finger.Value.Node);
                }

                this.Id.Runtime.Logger.WriteLine(" ... Printing keys of node {0}:", this.NodeId);
                foreach (var key in this.Keys)
                {
                    this.Id.Runtime.Logger.WriteLine("  >> Key-" + key);
                }
            }
        }

        private class Client : Machine
        {
            internal class Config : Event
            {
                public MachineId ClusterManager;
                public List<int> Keys;

                public Config(MachineId clusterManager, List<int> keys)
                    : base()
                {
                    this.ClusterManager = clusterManager;
                    this.Keys = keys;
                }
            }

            internal class Local : Event { }

            MachineId ClusterManager;

            List<int> Keys;
            int QueryCounter;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventGotoState(typeof(Local), typeof(Querying))]
            class Init : MachineState { }

            void InitOnEntry()
            {
                this.ClusterManager = (this.ReceivedEvent as Config).ClusterManager;
                this.Keys = (this.ReceivedEvent as Config).Keys;

                // LIVENESS BUG: can never detect the key, and keeps looping without
                // exiting the process. Enable to introduce the bug.
                this.Keys.Add(17);

                this.QueryCounter = 0;

                this.Raise(new Local());
            }

            [OnEntry(nameof(QueryingOnEntry))]
            [OnEventGotoState(typeof(Local), typeof(Waiting))]
            class Querying : MachineState { }

            void QueryingOnEntry()
            {
                if (this.QueryCounter < 1)
                {
                    var key = this.GetNextQueryKey();
                    this.Id.Runtime.Logger.WriteLine($"<ChordLog> Client is searching for successor of key '{key}'");
                    this.Send(this.ClusterManager, new ChordNode.FindSuccessor(this.Id, key));
                    this.Monitor<LivenessMonitor>(new LivenessMonitor.NotifyClientRequest(key));

                    this.QueryCounter++;
                }

                this.Raise(new Local());
            }

            int GetNextQueryKey()
            {
                int keyIndex = -1;
                while (keyIndex < 0)
                {
                    for (int i = 0; i < this.Keys.Count; i++)
                    {
                        if (this.Random())
                        {
                            keyIndex = i;
                            break;
                        }
                    }
                }

                return this.Keys[keyIndex];
            }

            [OnEventGotoState(typeof(Local), typeof(Querying))]
            [OnEventDoAction(typeof(ChordNode.FindSuccessorResp), nameof(ProcessFindSuccessorResp))]
            [OnEventDoAction(typeof(ChordNode.QueryIdResp), nameof(ProcessQueryIdResp))]
            class Waiting : MachineState { }

            void ProcessFindSuccessorResp()
            {
                var successor = (this.ReceivedEvent as ChordNode.FindSuccessorResp).Node;
                var key = (this.ReceivedEvent as ChordNode.FindSuccessorResp).Key;
                this.Monitor<LivenessMonitor>(new LivenessMonitor.NotifyClientResponse(key));
                this.Send(successor, new ChordNode.QueryId(this.Id));
            }

            void ProcessQueryIdResp()
            {
                this.Raise(new Local());
            }
        }

        private class LivenessMonitor : Monitor
        {
            public class NotifyClientRequest : Event
            {
                public int Key;

                public NotifyClientRequest(int key)
                    : base()
                {
                    this.Key = key;
                }
            }

            public class NotifyClientResponse : Event
            {
                public int Key;

                public NotifyClientResponse(int key)
                    : base()
                {
                    this.Key = key;
                }
            }

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            class Init : MonitorState { }

            void InitOnEntry()
            {
                this.Goto<Responded>();
            }

            [Cold]
            [OnEventGotoState(typeof(NotifyClientRequest), typeof(Requested))]
            class Responded : MonitorState { }

            [Hot]
            [OnEventGotoState(typeof(NotifyClientResponse), typeof(Responded))]
            class Requested : MonitorState { }
        }
    }
}

using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections;
using Celeste.Mod;

namespace CelesteBot_Everest_Interop
{
    // The Genome (Brain) for each Player.
    // This acts as the network, as well as contains various helper genetic functions.
    public class Genome
    {
        public ArrayList Genes = new ArrayList(); // All of the connections between Nodes
        public ArrayList Nodes = new ArrayList(); // All of the Nodes, in no particular order
        public int Inputs;
        public int Outputs;
        public int Layers = 2; // Default 2, increases as evolution occurs
        public int NextNode = 0; // The next Node ID to modify
        public int BiasNode; // The Node ID that represents the bias Node
        private Node bNode;

        public ArrayList network = new ArrayList();//a list of the nodes in the order that they need to be considered in the NN

        public Genome(int inp, int outp)
        {
            // Set input number and output number
            Inputs = inp;
            Outputs = outp;

            // Create input nodes: Node IDs are: [0-inputs)
            for (int i = 0; i < Inputs; i++)
            {
                Node temp = new Node(i);
                temp.Layer = 0;
                Nodes.Add(temp);
                NextNode++;
            }

            // Create output nodes: Node IDs are: [inputs-inputs+outputs)
            for (int i = 0; i < Outputs; i++)
            {
                Node temp = new Node(i + Inputs);
                temp.Layer = 1;
                Nodes.Add(temp);
                NextNode++;
            }
            // Creates bias Node: Node ID is: inputs+outputs
            bNode = new Node(NextNode);
            bNode.Layer = 0; // Bias Node part of input
            Nodes.Add(bNode);
            BiasNode = NextNode;
            NextNode++;
        }

        // Create an empty genome, used for crossover
        public Genome(int inp, int outp, bool crossover)
        {
            //set input number and output number
            Inputs = inp;
            Outputs = outp;
            // REMINDER TO SETUP BIAS NODE!
        }

        // Returns the Node with the matching ID
        public Node GetNode(int nodeID)
        {
            foreach (Node n in Nodes)
            {
                if (n.Id == nodeID)
                {
                    return n;
                }
            }
            return null;
        }

        // Adds the output connections of each Node
        public void ConnectNodes()
        {
            // Clears all of the Node connections so that they can be reconnected using the GeneConnection array instead.
            foreach (Node n in Nodes)
            {//clear the connections
                n.OutputConnections.Clear();
            }
            // Reconnect each Node using the GeneConnection array
            foreach (GeneConnection g in Genes)
            {
                g.FromNode.OutputConnections.Add(g);
            }
        }

        // Expects input array, returns output of NN
        public float[] FeedForward(float[] inputValues)
        {
            if (network == null)
            {
                throw new Exception("Network hasn't been initialized yet, but you are trying to feedForward it!");
            }

            // Set the outputs of the input nodes
            for (int i = 0; i < Inputs; i++)
            {
                Node temp = (Node)Nodes[i];
                temp.OutputValue = inputValues[i];
            }
            bNode.OutputValue = 1; // Bias = 1

            // Activate each Node in the network
            foreach (Node n in network)
            {
                n.Activate();
            }

            // Output Node IDs are [inputs, inputs+outputs)
            float[] outs = new float[Outputs];
            for (int i = 0; i < Outputs; i++)
            {
                Node n = (Node)Nodes[Inputs + i];
                outs[i] = n.OutputValue;
            }

            // Reset
            foreach (Node n in Nodes)
            {
                n.InputSum = 0;
            }

            return outs;
        }

        // Sets up the NN as a list of nodes in the correct order to be actiavted 
        public void GenerateNetwork()
        {
            ConnectNodes();
            network = new ArrayList();
            // For each layer: For each Node in the nodes array: If their layer matches, add it to the network.
            // This will add the Nodes in order of layer, then ID

            for (int l = 0; l < Layers; l++)
            {
                foreach (Node n in Nodes)
                {
                    if (n.Layer == l)
                    {
                        network.Add(n);
                    }
                }
            }
        }

        // Mutate the NN by adding a new Node
        // Randomly disable a GeneConnection, then create two new GeneConnections between the input Node and the new Node + the new Node and the output Node 
        public void AddNode(ArrayList innovationHistory)
        {
            // Pick a random connection to create a Node between
            if (Genes.Count == 0)
            {
                // If there are NO GeneConnections
                AddConnection(innovationHistory);
                // Add a Connection from previous Genomes
                return;
            }
            Random rand = new Random(Guid.NewGuid().GetHashCode());
            int randomConnection = rand.Next(Genes.Count);

            GeneConnection temp = (GeneConnection)Genes[randomConnection];
            while (temp.FromNode == bNode && Genes.Count != 1)
            {// Bias must remain connected
                randomConnection = rand.Next(Genes.Count);
                temp = (GeneConnection)Genes[randomConnection];
            }

            temp.Enabled = false;

            int newNodeNo = NextNode; // nextNode is STILL the next ID to add
            Node toAdd = new Node(newNodeNo);
            NextNode++;

            // Gets the innovationNumber of this new GeneConnection between the input Node and the new Node
            int connectionInnovationNumber = GetInnovationNumber(innovationHistory, temp.FromNode, toAdd);
            // Add a new GeneConnection to the new Node with a weight of 1
            Genes.Add(new GeneConnection(temp.FromNode, toAdd, 1, connectionInnovationNumber));

            // Gets the innovationNumber of this new GeneConnection between the new Node and the output Node
            connectionInnovationNumber = GetInnovationNumber(innovationHistory, toAdd, temp.ToNode);

            // Add a new GeneConnection from the new node with a weight the same as the disabled connection
            Genes.Add(new GeneConnection(toAdd, temp.ToNode, temp.Weight, connectionInnovationNumber));
            toAdd.Layer = temp.FromNode.Layer + 1; // The original output Node gets shifted down 1 layer

            // Gets the innovationNumber of a new GeneConnection between the bias Node and the new Node
            connectionInnovationNumber = GetInnovationNumber(innovationHistory, bNode, toAdd);
            // Connect the bias to the new node with a weight of 0
            Genes.Add(new GeneConnection(bNode, toAdd, 0, connectionInnovationNumber));

            // If the layer of the new Node is equal to the layer of the output Node, a new layer must be created.
            // All of the layers of all of the Nodes with layers >= the new Node's layer must be incremented
            if (toAdd.Layer == temp.ToNode.Layer)
            {
                foreach (Node n in Nodes)
                { // Make sure not to include the new Node (last Node in nodes)
                    if (n.Layer >= toAdd.Layer)
                    {
                        n.Layer++;
                    }
                }
                Layers++;
            }
            Nodes.Add(toAdd);
            ConnectNodes(); // Reconnect the Nodes after this has been created
        }

        // Adds a connection between 2 nodes which aren't currently connected
        public void AddConnection(ArrayList innovationHistory)
        {
            // Cannot add a connection to a fully connected network
            if (FullyConnected())
            {
                Logger.Log(CelesteBotInteropModule.ModLogKey, "Cannot add a connection because the network is fully connected!");
                return;
            }
            Random rand = new Random(Guid.NewGuid().GetHashCode());
            // Get random nodes
            int randomNode1 = rand.Next(Nodes.Count);
            int randomNode2 = rand.Next(Nodes.Count);
            while (IsNonUnique(randomNode1, randomNode2))
            {// While the random Node indicies are non Unique
             // Get new Nodes
                randomNode1 = rand.Next(Nodes.Count);
                randomNode2 = rand.Next(Nodes.Count);
            }
            // If the first random Node is after the second then switch the first and second Nodes
            Node temp;
            Node node1 = (Node)Nodes[randomNode1];
            Node node2 = (Node)Nodes[randomNode2];
            if (node1.Layer > node2.Layer)
            {
                temp = node2;
                node2 = node1;
                node1 = temp;
            }

            // Gets the innovation number of this new connection
            int connectionInnovationNumber = GetInnovationNumber(innovationHistory, node1, node2);
            // Add the connection with a random weight
            Genes.Add(new GeneConnection(node1, node2, (float)(rand.NextDouble() - 0.5) * (float)(CelesteBotManager.WEIGHT_MAXIMUM * 2), connectionInnovationNumber));
            ConnectNodes(); // Reconnect the Nodes
        }

        // Returns if the two Node indicies are non Unique
        public bool IsNonUnique(int r1, int r2)
        {
            Node n1 = (Node)Nodes[r1];
            Node n2 = (Node)Nodes[r2];
            if (n1.Layer == n2.Layer) return true; // If the nodes are in the same layer 
            if (n1.IsConnectedTo(n2)) return true; // If the nodes are already connected
            return false;
        }

        // Returns the innovation number for the given Connection
        // If this mutation has never been seen before then it will be given a new, unique innovation number
        // If this mutation matches a previous mutation then it will be given the same innovation number as the previous one
        public int GetInnovationNumber(ArrayList innovationHistory, Node from, Node to)
        {
            // nextConnectionNumber is a public, global variable because all the Genomes should share innovationNumber Uniqueness.
            // In other words, all the different Genomes could mutate unique innovationNumbers, but that should be reflected.
            if (from == null || to == null)
            {
                Logger.Log(CelesteBotInteropModule.ModLogKey, "WAIT HOW DID THIS HAPPEN!?");
                Logger.Log(CelesteBotInteropModule.ModLogKey, innovationHistory.ToString());
            }
            bool isNew = true;
            int connectionInnovationNumber = ConnectionHistory.NextConnectionInnovationNumber;
            foreach (ConnectionHistory h in innovationHistory)
            { // For each previous mutation
                if (h.Matches(this, from, to))
                { // If match found
                    isNew = false;// The Connection is not unique/new
                    connectionInnovationNumber = h.InnovationNumber; // Set the innovation number as the innovation number of the match
                    break;
                }
            }

            if (isNew)
            { // If the mutation is new then create an ArrayList of integers representing the current state of the genome
                ArrayList currentGenomeState = new ArrayList();
                foreach (GeneConnection g in Genes)
                { // Set the innovation numbers
                    currentGenomeState.Add(g.InnovationNo);
                }
                
                // Then add this unique Connection to innovationHistory
                innovationHistory.Add(new ConnectionHistory(from.Id, to.Id, connectionInnovationNumber, currentGenomeState));
                ConnectionHistory.NextConnectionInnovationNumber++;
            }
            return connectionInnovationNumber;
        }

        // Returns whether the network is fully connected or not
        public bool FullyConnected()
        {
            int maxConnections = 0;
            int[] nodesInLayers = new int[Layers]; // Array which stores the amount of nodes in each layer

            foreach (Node n in Nodes)
            {
                nodesInLayers[n.Layer] += 1;
            }

            // For each layer the maximum amount of connections is the number of Nodes in this layer * the number of Nodes one layer in front of it
            // Add the up all of these for each layer to get maxConnections
            for (int i = 0; i < Layers - 1; i++)
            {
                int nodesInFront = 0;
                for (int j = i + 1; j < Layers; j++)
                {//for each layer infront of this layer
                    nodesInFront += nodesInLayers[j];//add up nodes
                }
                maxConnections += nodesInLayers[i] * nodesInFront;
            }

            // If the number of connections is equal to the max number of connections possible then it is full
            if (maxConnections == Genes.Count)
            {
                return true;
            }
            return false;
        }

        // Mutates the genome
        public void Mutate(ArrayList innovationHistory)
        {
            // If there are no GeneConnections, add a random one from history
            if (Genes.Count == 0)
            {
                AddConnection(innovationHistory);
            }
            // Randomly choose to mutate the weight
            Random rand = new Random(Guid.NewGuid().GetHashCode());
            double rand1 = rand.NextDouble();
            if (rand1 < CelesteBotManager.WEIGHT_MUTATION_CHANCE)
            {
                foreach (GeneConnection g in Genes)
                {
                    g.MutateWeight();
                }
            }
            // Randomly choose to add a GeneConnection
            double rand2 = rand.NextDouble();
            if (rand2 < CelesteBotManager.ADD_CONNECTION_CHANCE)
            {
                AddConnection(innovationHistory);
            }
            // Randomly choose to add a Node
            double rand3 = rand.NextDouble();
            if (rand3 < CelesteBotManager.ADD_NODE_CHANCE)
            {
                AddNode(innovationHistory);
            }
        }

        // Performs crossover, assuming that this Genome is more fit than the other Genome
        public Genome Crossover(Genome parent2)
        {
            Genome child = new Genome(Inputs, Outputs, true);
            child.Genes.Clear();
            child.Nodes.Clear();
            child.Layers = Layers;
            child.NextNode = NextNode;
            child.BiasNode = BiasNode;
            ArrayList childGenes = new ArrayList(); // This will serve as a list of GeneConnections to inherit from parents
                                                    // Remove this array soon...
            ArrayList isEnabled = new ArrayList();  // All of the enabled/disabled Nodes (because why would I make each Node have an enabled/disabled tag...)

            Random random = new Random(Guid.NewGuid().GetHashCode());
            // All genes
            foreach (GeneConnection g in Genes)
            {
                bool setEnabled = true; // Is this node in the chlid going to be enabled

                int parent2gene = parent2.MatchingGene(g.InnovationNo);
                if (parent2gene != -1)
                { // If the gene does not match between parents
                    GeneConnection g2 = (GeneConnection)parent2.Genes[parent2gene];
                    if (!g.Enabled || !g2.Enabled)
                    {// If either of the matching genes are disabled
                     // Punnet square math! 75% of time disable child's gene
                        if (random.NextDouble() < 0.75)
                        {
                            setEnabled = false;
                        }
                    }
                    double rand = random.NextDouble();
                    if (rand < 0.5)
                    {
                        // Punnet square math! 50% of time get Gene from Parent1
                        childGenes.Add(g);
                    }
                    else
                    {
                        // Punnet square math! 50% of time get Gene from Parent2
                        childGenes.Add(g2);
                    }
                }
                else
                { // This gene already exists in both parents, take from more fit parent
                    childGenes.Add(g);
                    setEnabled = g.Enabled;
                }
                isEnabled.Add(setEnabled);
            }

            // Since all excess and disjoint genes are inherrited from the more fit parent (this Genome) the child's Node structure is no different from this parent
            // So all of the child's Nodes can be inherrited from this parent
            foreach (Node n in Nodes)
            {
                child.Nodes.Add(n.Clone());
            }

            // Clone all the connections so that they connect the childs new nodes
            for (int i = 0; i < childGenes.Count; i++)
            {
                GeneConnection g = (GeneConnection)childGenes[i];
                child.Genes.Add(g.Clone(child.GetNode(g.FromNode.Id), child.GetNode(g.ToNode.Id)));
                g.Enabled = (bool)isEnabled[i]; // Please remove this
            }
            child.bNode = child.GetNode(child.BiasNode);
            child.ConnectNodes();
            return child;
        }

        // Returns whether or not there is a gene matching the input innovation number in the provided Genome
        public int MatchingGene(int innovationNumber)
        {
            for (int i = 0; i < Genes.Count; i++)
            {
                GeneConnection g = (GeneConnection)Genes[i];
                if (g.InnovationNo == innovationNumber)
                {
                    return i;
                }
            }
            return -1; //no matching gene found
        }

        // Prints outp info about the genome to the console 
        public void PrintGenome()
        {
            Logger.Log(CelesteBotInteropModule.ModLogKey, "Genome layers: "+ Layers);
            Logger.Log(CelesteBotInteropModule.ModLogKey, "Bias node: " + BiasNode);
            Logger.Log(CelesteBotInteropModule.ModLogKey, "Node IDs: ");
            foreach (Node n in Nodes)
            {
                Console.Write(n.Id + ",");
            }
            Logger.Log(CelesteBotInteropModule.ModLogKey, "Genes");
            foreach (GeneConnection g in Genes)
            {//for each GeneConnection
                Logger.Log(CelesteBotInteropModule.ModLogKey, "gene " + g.InnovationNo + " From node " + g.FromNode.Id + " To node " + g.ToNode.Id +
                  " is enabled " + g.Enabled + " from layer " + g.FromNode.Layer + " to layer " + g.ToNode.Layer + " weight: " + g.Weight);
            }
        }

        // Returns a clone of this genome
        public Genome Clone()
        {
            Genome clone = new Genome(Inputs, Outputs, true);

            foreach (Node n in Nodes)
            {
                clone.Nodes.Add(n.Clone());
            }
            // Copy all the connections so that they connect the new Nodes in the Clone
            foreach (GeneConnection g in Genes)
            {
                clone.Genes.Add(g.Clone(clone.GetNode(g.FromNode.Id), clone.GetNode(g.ToNode.Id)));
            }

            clone.Layers = Layers;
            clone.NextNode = NextNode;
            clone.BiasNode = BiasNode;
            clone.bNode = bNode.Clone();
            clone.ConnectNodes();
            clone.GenerateNetwork();

            return clone;
        }

        public override string ToString()
        {
            string outp = "GENOME<<";
            outp += "I:" + Inputs + ", ";
            outp += "O:" + Outputs + ", ";
            outp += "NODE<";
            foreach (Node n in Nodes)
            {
                outp += n + ", ";
            }
            outp += ">, GENES<";
            foreach (GeneConnection g in Genes)
            {
                outp += g + ", ";
            }
            outp += ">, Layers:" + Layers + ", ";
            outp += "nextNode:" + NextNode + ", ";
            outp += "biasNode:" + BiasNode + ">>";
            return outp;
        }
        public static Genome GenomeFromString(string str)
        {
            try
            {
                str = str.Split(new string[] { "GENOME<<" }, StringSplitOptions.None)[1];
                int inputs = Convert.ToInt32(str.Split(new string[] { "I:" }, StringSplitOptions.None)[1].Split(new string[] { ", " }, StringSplitOptions.None)[0]);
                int outputs = Convert.ToInt32(str.Split(new string[] { "O:" }, StringSplitOptions.None)[1].Split(new string[] { ", " }, StringSplitOptions.None)[0]);
                ArrayList nodes = new ArrayList();
                string forNodes = str.Split(new string[] { "NODE<" }, StringSplitOptions.None)[1].Split(new string[] { ">, GENES<" }, StringSplitOptions.None)[0];
                while (forNodes.Contains("N<"))
                {
                    nodes.Add(Node.NodeFromString(forNodes));
                    forNodes = forNodes.Substring(forNodes.IndexOf(">") + 1, forNodes.Length);
                }
                ArrayList genes = new ArrayList();
                string forGenes = str.Split(new string[] { "GENES<" }, StringSplitOptions.None)[1].Split(new string[] { ">, Layers:" }, StringSplitOptions.None)[0];
                while (forGenes.Contains("G<"))
                {
                    string forGene1 = forGenes.Substring(0, forGenes.IndexOf("e>") + 1);
                    genes.Add(GeneConnection.GeneFromString(forGene1));
                    forGenes = forGenes.Substring(forGenes.IndexOf("e>") + 3, forGenes.Length);
                }
                int layers = Convert.ToInt32(str.Split(new string[] { "Layers:" }, StringSplitOptions.None)[1].Split(new string[] { ", " }, StringSplitOptions.None)[0]);
                int nextNode = Convert.ToInt32(str.Split(new string[] { "nextNode:" }, StringSplitOptions.None)[1].Split(new string[] { ", " }, StringSplitOptions.None)[0]);
                int biasNode = Convert.ToInt32(str.Split(new string[] { "biasNode:" }, StringSplitOptions.None)[1].Split(new string[] { ">>" }, StringSplitOptions.None)[0]);

                Genome clone = new Genome(inputs, outputs, true);

                foreach (Node n in nodes)
                {
                    clone.Nodes.Add(n.Clone());
                }
                // Copy all the connections so that they connect the new Nodes in the Clone
                foreach (GeneConnection g in genes)
                {
                    clone.Genes.Add(g.Clone(clone.GetNode(g.FromNode.Id), clone.GetNode(g.ToNode.Id)));
                }

                clone.Layers = layers;
                clone.NextNode = nextNode;
                clone.BiasNode = biasNode;
                clone.ConnectNodes();
                clone.GenerateNetwork();

                return clone;

            }
            catch (Exception e)
            {
                Logger.Log(CelesteBotInteropModule.ModLogKey, e.Message);
                return null;
            }
        }
    }
}

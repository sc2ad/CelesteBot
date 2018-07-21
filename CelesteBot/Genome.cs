using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections;

namespace CelesteBot
{
    // The Genome (Brain) for each Player.
    // This acts as the network, as well as contains various helper genetic functions.
    public class Genome
    {
        public ArrayList genes = new ArrayList(); // All of the connections between Nodes
        public ArrayList nodes = new ArrayList(); // All of the Nodes, in no particular order
        public int inputs;
        public int outputs;
        public int layers = 2; // Default 2, increases as evolution occurs
        public int nextNode = 0; // The next Node ID to modify
        public int biasNode; // The Node ID that represents the bias Node
        private Node bNode;

        public ArrayList network = new ArrayList();//a list of the nodes in the order that they need to be considered in the NN

        public Genome(int inp, int outp)
        {
            // Set input number and output number
            inputs = inp;
            outputs = outp;

            // Create input nodes: Node IDs are: [0-inputs)
            for (int i = 0; i < inputs; i++)
            {
                Node temp = new Node(i);
                temp.layer = 0;
                nodes.Add(new Node(i));
                nextNode++;
            }

            // Create output nodes: Node IDs are: [inputs-inputs+outputs)
            for (int i = 0; i < outputs; i++)
            {
                Node temp = new Node(i + inputs);
                temp.layer = 1;
                nodes.Add(new Node(i + inputs));
                nextNode++;
            }
            // Creates bias Node: Node ID is: inputs+outputs
            nodes.Add(new Node(nextNode));
            biasNode = nextNode;
            nextNode++;
            Node bNode = (Node)nodes[biasNode];
            bNode.layer = 0; // Bias Node part of input layer
            this.bNode = bNode;
        }

        // Create an empty genome, used for crossover
        public Genome(int inp, int outp, bool crossover)
        {
            //set input number and output number
            inputs = inp;
            outputs = outp;
            // REMINDER TO SETUP BIAS NODE!
        }

        // Returns the Node with the matching ID
        public Node getNode(int nodeID)
        {
            foreach (Node n in nodes)
            {
                if (n.id == nodeID)
                {
                    return n;
                }
            }
            return null;
        }

        // Adds the output connections of each Node
        public void connectNodes()
        {
            // Clears all of the Node connections so that they can be reconnected using the GeneConnection array instead.
            foreach (Node n in nodes)
            {//clear the connections
                n.outputConnections.Clear();
            }
            // Reconnect each Node using the GeneConnection array
            foreach (GeneConnection g in genes)
            {
                g.fromNode.outputConnections.Add(g);
            }
        }

        // Expects input array, returns output of NN
        public float[] feedForward(float[] inputValues)
        {
            if (network == null)
            {
                throw new Exception("Network hasn't been initialized yet, but you are trying to feedForward it!");
            }

            // Set the outputs of the input nodes
            for (int i = 0; i < inputs; i++)
            {
                Node temp = (Node)nodes[i];
                temp.outputValue = inputValues[i];
            }
            bNode.outputValue = 1; // Bias = 1

            // Activate each Node in the network
            foreach (Node n in network)
            {
                n.activate();
            }

            // Output Node IDs are [inputs, inputs+outputs)
            float[] outs = new float[outputs];
            for (int i = 0; i < outputs; i++)
            {
                Node n = (Node)nodes[inputs + i];
                outs[i] = n.outputValue;
            }

            // Reset
            foreach (Node n in nodes)
            {
                n.inputSum = 0;
            }

            return outs;
        }

        // Sets up the NN as a list of nodes in the correct order to be actiavted 
        public void generateNetwork()
        {
            connectNodes();
            network = new ArrayList();
            // For each layer: For each Node in the nodes array: If their layer matches, add it to the network.
            // This will add the Nodes in order of layer, then ID

            for (int l = 0; l < layers; l++)
            {
                foreach (Node n in nodes)
                {
                    if (n.layer == l)
                    {
                        network.Add(n);
                    }
                }
            }
        }

        // Mutate the NN by adding a new Node
        // Randomly disable a GeneConnection, then create two new GeneConnections between the input Node and the new Node + the new Node and the output Node 
        public void addNode(ArrayList innovationHistory)
        {
            // Pick a random connection to create a Node between
            if (genes.Count == 0)
            {
                // If there are NO GeneConnections
                addConnection(innovationHistory);
                // Add a Connection from previous Genomes
                return;
            }
            Random rand = new Random();
            int randomConnection = rand.Next(genes.Count);

            GeneConnection temp = (GeneConnection)genes[randomConnection];
            while (temp.fromNode == bNode && genes.Count != 1)
            {// Bias must remain connected
                randomConnection = rand.Next(genes.Count);
                temp = (GeneConnection)genes[randomConnection];
            }

            temp.enabled = false;

            int newNodeNo = nextNode; // nextNode is STILL the next ID to add
            nodes.Add(new Node(newNodeNo));
            nextNode++;

            // Gets the innovationNumber of this new GeneConnection between the input Node and the new Node
            int connectionInnovationNumber = getInnovationNumber(innovationHistory, temp.fromNode, getNode(newNodeNo));
            // Add a new GeneConnection to the new Node with a weight of 1
            genes.Add(new GeneConnection(temp.fromNode, getNode(newNodeNo), 1, connectionInnovationNumber));

            // Gets the innovationNumber of this new GeneConnection between the new Node and the output Node
            connectionInnovationNumber = getInnovationNumber(innovationHistory, getNode(newNodeNo), temp.toNode);

            // Add a new GeneConnection from the new node with a weight the same as the disabled connection
            genes.Add(new GeneConnection(getNode(newNodeNo), temp.toNode, temp.weight, connectionInnovationNumber));
            getNode(newNodeNo).layer = temp.fromNode.layer + 1; // The original output Node gets shifted down 1 layer

            // Gets the innovationNumber of a new GeneConnection between the bias Node and the new Node
            connectionInnovationNumber = getInnovationNumber(innovationHistory, bNode, getNode(newNodeNo));
            // Connect the bias to the new node with a weight of 0
            genes.Add(new GeneConnection(bNode, getNode(newNodeNo), 0, connectionInnovationNumber));

            // If the layer of the new Node is equal to the layer of the output Node, a new layer must be created.
            // All of the layers of all of the Nodes with layers >= the new Node's layer must be incremented
            if (getNode(newNodeNo).layer == temp.toNode.layer)
            {
                foreach (Node n in nodes)
                { // Make sure not to include the new Node (last Node in nodes)
                    if (n.layer >= getNode(newNodeNo).layer)
                    {
                        n.layer++;
                    }
                }
                layers++;
            }
            connectNodes(); // Reconnect the Nodes after this has been created
        }

        // Adds a connection between 2 nodes which aren't currently connected
        public void addConnection(ArrayList innovationHistory)
        {
            // Cannot add a connection to a fully connected network
            if (fullyConnected())
            {
                Console.WriteLine("Cannot add a connection because the network is fully connected!");
                return;
            }
            Random rand = new Random();
            // Get random nodes
            int randomNode1 = rand.Next(nodes.Count);
            int randomNode2 = rand.Next(nodes.Count);
            while (isNonUnique(randomNode1, randomNode2))
            {// While the random Node indicies are non Unique
             // Get new Nodes
                randomNode1 = rand.Next(nodes.Count);
                randomNode2 = rand.Next(nodes.Count);
            }
            // If the first random Node is after the second then switch the first and second Nodes
            int temp;
            Node node1 = (Node)nodes[randomNode1];
            Node node2 = (Node)nodes[randomNode2];
            if (node1.layer > node2.layer)
            {
                temp = randomNode2;
                randomNode2 = randomNode1;
                randomNode1 = temp;
            }

            // Gets the innovation number of this new connection
            int connectionInnovationNumber = getInnovationNumber(innovationHistory, node1, node2);
            // Add the connection with a random weight
            genes.Add(new GeneConnection(node1, node2, (float)(rand.NextDouble() - 0.5) * 2, connectionInnovationNumber));
            connectNodes(); // Reconnect the Nodes
        }

        // Returns if the two Node indicies are non Unique
        public bool isNonUnique(int r1, int r2)
        {
            Node n1 = (Node)nodes[r1];
            Node n2 = (Node)nodes[r2];
            if (n1.layer == n2.layer) return true; // If the nodes are in the same layer 
            if (n1.isConnectedTo(n2)) return true; // If the nodes are already connected
            return false;
        }

        // Returns the innovation number for the given Connection
        // If this mutation has never been seen before then it will be given a new, unique innovation number
        // If this mutation matches a previous mutation then it will be given the same innovation number as the previous one
        public int getInnovationNumber(ArrayList innovationHistory, Node from, Node to)
        {
            // nextConnectionNumber is a public, global variable because all the Genomes should share innovationNumber Uniqueness.
            // In other words, all the different Genomes could mutate unique innovationNumbers, but that should be reflected.
            bool isNew = true;
            int connectionInnovationNumber = ConnectionHistory.nextConnectionInnovationNumber;
            foreach (ConnectionHistory h in innovationHistory)
            { // For each previous mutation
                if (h.matches(this, from, to))
                { // If match found
                    isNew = false;// The Connection is not unique/new
                    connectionInnovationNumber = h.innovationNumber; // Set the innovation number as the innovation number of the match
                    break;
                }
            }

            if (isNew)
            { // If the mutation is new then create an ArrayList of integers representing the current state of the genome
                ArrayList currentGenomeState = new ArrayList();
                foreach (GeneConnection g in genes)
                { // Set the innovation numbers
                    currentGenomeState.Add(g.innovationNo);
                }

                // Then add this unique Connection to innovationHistory
                innovationHistory.Add(new ConnectionHistory(from.id, to.id, connectionInnovationNumber, currentGenomeState));
                ConnectionHistory.nextConnectionInnovationNumber++;
            }
            return connectionInnovationNumber;
        }

        // Returns whether the network is fully connected or not
        public bool fullyConnected()
        {
            int maxConnections = 0;
            int[] nodesInLayers = new int[layers]; // Array which stores the amount of nodes in each layer

            foreach (Node n in nodes)
            {
                nodesInLayers[n.layer] += 1;
            }

            // For each layer the maximum amount of connections is the number of Nodes in this layer * the number of Nodes one layer in front of it
            // Add the up all of these for each layer to get maxConnections
            for (int i = 0; i < layers - 1; i++)
            {
                int nodesInFront = 0;
                for (int j = i + 1; j < layers; j++)
                {//for each layer infront of this layer
                    nodesInFront += nodesInLayers[j];//add up nodes
                }
                maxConnections += nodesInLayers[i] * nodesInFront;
            }

            // If the number of connections is equal to the max number of connections possible then it is full
            if (maxConnections == genes.Count)
            {
                return true;
            }
            return false;
        }

        // Mutates the genome
        public void mutate(ArrayList innovationHistory)
        {
            // If there are no GeneConnections, add a random one from history
            if (genes.Count == 0)
            {
                addConnection(innovationHistory);
            }
            // Randomly choose to mutate the weight
            Random rand = new Random();
            double rand1 = rand.NextDouble();
            if (rand1 < Manager.WEIGHT_MUTATION_CHANCE)
            {
                foreach (GeneConnection g in genes)
                {
                    g.mutateWeight();
                }
            }
            // Randomly choose to add a GeneConnection
            double rand2 = rand.NextDouble();
            if (rand2 < Manager.ADD_CONNECTION_CHANCE)
            {
                addConnection(innovationHistory);
            }
            // Randomly choose to add a Node
            double rand3 = rand.NextDouble();
            if (rand3 < Manager.ADD_NODE_CHANCE)
            {
                addNode(innovationHistory);
            }
        }

        // Performs crossover, assuming that this Genome is more fit than the other Genome
        public Genome crossover(Genome parent2)
        {
            Genome child = new Genome(inputs, outputs, true);
            child.genes.Clear();
            child.nodes.Clear();
            child.layers = layers;
            child.nextNode = nextNode;
            child.biasNode = biasNode;
            ArrayList childGenes = new ArrayList(); // This will serve as a list of GeneConnections to inherit from parents
                                                    // Remove this array soon...
            ArrayList isEnabled = new ArrayList();  // All of the enabled/disabled Nodes (because why would I make each Node have an enabled/disabled tag...)

            Random random = new Random();
            // All genes
            foreach (GeneConnection g in genes)
            {
                bool setEnabled = true; // Is this node in the chlid going to be enabled

                int parent2gene = parent2.matchingGene(g.innovationNo);
                if (parent2gene != -1)
                { // If the gene does not match between parents
                    GeneConnection g2 = (GeneConnection)parent2.genes[parent2gene];
                    if (!g.enabled || !g2.enabled)
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
                    setEnabled = g.enabled;
                }
                isEnabled.Add(setEnabled);
            }

            // Since all excess and disjoint genes are inherrited from the more fit parent (this Genome) the child's Node structure is no different from this parent
            // So all of the child's Nodes can be inherrited from this parent
            foreach (Node n in nodes)
            {
                child.nodes.Add(n.clone());
            }

            // Clone all the connections so that they connect the childs new nodes
            for (int i = 0; i < childGenes.Count; i++)
            {
                GeneConnection g = (GeneConnection)childGenes[i];
                child.genes.Add(g.clone(child.getNode(g.fromNode.id), child.getNode(g.toNode.id)));
                g.enabled = (bool)isEnabled[i]; // Please remove this
            }
            child.connectNodes();
            return child;
        }

        // Returns whether or not there is a gene matching the input innovation number in the provided Genome
        public int matchingGene(int innovationNumber)
        {
            for (int i = 0; i < genes.Count; i++)
            {
                GeneConnection g = (GeneConnection)genes[i];
                if (g.innovationNo == innovationNumber)
                {
                    return i;
                }
            }
            return -1; //no matching gene found
        }

        // Prints outp info about the genome to the console 
        public void printGenome()
        {
            Console.WriteLine("Genome layers: ", layers);
            Console.WriteLine("Bias node: " + biasNode);
            Console.WriteLine("Node IDs: ");
            foreach (Node n in nodes)
            {
                Console.Write(n.id + ",");
            }
            Console.WriteLine("Genes");
            foreach (GeneConnection g in genes)
            {//for each GeneConnection
                Console.WriteLine("gene " + g.innovationNo, "From node " + g.fromNode.id, "To node " + g.toNode.id,
                  "is enabled " + g.enabled, "from layer " + g.fromNode.layer, "to layer " + g.toNode.layer, "weight: " + g.weight);
            }
            Console.WriteLine();
        }

        // Returns a clone of this genome
        public Genome clone()
        {
            Genome clone = new Genome(inputs, outputs, true);

            foreach (Node n in nodes)
            {
                clone.nodes.Add(n.clone());
            }
            // Copy all the connections so that they connect the new Nodes in the Clone
            foreach (GeneConnection g in genes)
            {
                clone.genes.Add(g.clone(clone.getNode(g.fromNode.id), clone.getNode(g.toNode.id)));
            }

            clone.layers = layers;
            clone.nextNode = nextNode;
            clone.biasNode = biasNode;
            clone.bNode = bNode.clone();
            clone.connectNodes();
            clone.generateNetwork();

            return clone;
        }

        public override string ToString()
        {
            string outp = "GENOME<<";
            outp += "I:" + inputs + ", ";
            outp += "O:" + outputs + ", ";
            outp += "NODE<";
            foreach (Node n in nodes)
            {
                outp += n + ", ";
            }
            outp += ">, GENES<";
            foreach (GeneConnection g in genes)
            {
                outp += g + ", ";
            }
            outp += ">, Layers:" + layers + ", ";
            outp += "nextNode:" + nextNode + ", ";
            outp += "biasNode:" + biasNode + ">>";
            return outp;
        }
        public static Genome genomeFromString(string str)
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
                    nodes.Add(Node.nodeFromString(forNodes));
                    forNodes = forNodes.Substring(forNodes.IndexOf(">") + 1, forNodes.Length);
                }
                ArrayList genes = new ArrayList();
                string forGenes = str.Split(new string[] { "GENES<" }, StringSplitOptions.None)[1].Split(new string[] { ">, Layers:" }, StringSplitOptions.None)[0];
                while (forGenes.Contains("G<"))
                {
                    string forGene1 = forGenes.Substring(0, forGenes.IndexOf("e>") + 1);
                    genes.Add(GeneConnection.geneFromString(forGene1));
                    forGenes = forGenes.Substring(forGenes.IndexOf("e>") + 3, forGenes.Length);
                }
                int layers = Convert.ToInt32(str.Split(new string[] { "Layers:" }, StringSplitOptions.None)[1].Split(new string[] { ", " }, StringSplitOptions.None)[0]);
                int nextNode = Convert.ToInt32(str.Split(new string[] { "nextNode:" }, StringSplitOptions.None)[1].Split(new string[] { ", " }, StringSplitOptions.None)[0]);
                int biasNode = Convert.ToInt32(str.Split(new string[] { "biasNode:" }, StringSplitOptions.None)[1].Split(new string[] { ">>" }, StringSplitOptions.None)[0]);

                Genome clone = new Genome(inputs, outputs, true);

                foreach (Node n in nodes)
                {
                    clone.nodes.Add(n.clone());
                }
                // Copy all the connections so that they connect the new Nodes in the Clone
                foreach (GeneConnection g in genes)
                {
                    clone.genes.Add(g.clone(clone.getNode(g.fromNode.id), clone.getNode(g.toNode.id)));
                }

                clone.layers = layers;
                clone.nextNode = nextNode;
                clone.biasNode = biasNode;
                clone.connectNodes();
                clone.generateNetwork();

                return clone;

            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return null;
            }
        }
    }
}

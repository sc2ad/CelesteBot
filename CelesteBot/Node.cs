using Microsoft.Xna.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CelesteBot
{
    // This class represents a Node (or a neuron)
    public class Node
    {
        public int id; // The ID of the Node (neuron), which is ALWAYS unique
        public float inputSum = 0; // rewriten by all Nodes (neurons) that have this node (neuron) as an output. This is before activation.
        public float outputValue = 0; // Output value to send to all Output Nodes (neurons)
        public ArrayList outputConnections = new ArrayList(); // All of the outputs of this Node (neuron)
        public int layer = 0; // Where is the Node (neuron)? Layer 0 = input, Layer LAST = output
        public Vector2 drawPos = new Vector2(); // For drawing (Genome)

        public Node(int no)
        {
            // Only ID is set on construction, everything else is mutated externally as Genome sees fit
            id = no;
        }
        // Activates the Node and relays output to future Nodes
        public void activate()
        {
            // If not the input layer
            if (layer != 0)
            {
                outputValue = sigmoid(inputSum);
            }
            // Send the outputValue * weight to each of the output Nodes of this Node
            for (int i = 0; i < outputConnections.Count; i++)
            {
                GeneConnection temp = (GeneConnection)outputConnections[i];
                if (temp.enabled)
                {
                    temp.toNode.inputSum += temp.weight * outputValue;
                }
                outputConnections[i] = temp;
            }
        }
        // Simple Step
        public float stepFunction(float x)
        {
            if (x < 0)
            {
                return 0;
            }
            else
            {
                return 1;
            }
        }
        // reLU activation function
        public float relu(float x)
        {
            return Math.Max(x, 0);
        }
        // Sigmoid
        public float sigmoid(float x)
        {
            float y = 1 / (1 + (float)Math.Pow((float)Math.E, -4.9 * x));
            return y;
        }
        // Returns whether this node is connected to the parameter node
        public bool isConnectedTo(Node node)
        {
            if (node.layer == layer)
            {//nodes in the same layer cannot be connected
                return false;
            }

            // If the other Node comes BEFORE this Node, check to see if this Node is an output of that Node
            if (node.layer < layer)
            {
                foreach (GeneConnection g in outputConnections)
                {
                    if (g.toNode == this)
                    {
                        return true;
                    }
                }
            }
            // Otherwise check to see if the other Node is an output of this Node
            else
            {
                foreach(GeneConnection g in outputConnections)
                {
                    if (g.toNode == node)
                    {
                        return true;
                    }
                }
            }
            return false;
        }
        // Clone the Node
        public Node clone()
        {
            Node clone = new Node(id);
            clone.layer = layer;
            return clone;
        }
        public override string ToString()
        {
            return "N<" + id + ", " + layer + ">";
        }
        public static Node nodeFromString(string str)
        {
            try
            {
                string[] split = str.Split(new string[] { "N<" }, StringSplitOptions.None)[1].Split(new string[] { ">" }, StringSplitOptions.None)[0].Split(new string[] { ", " }, StringSplitOptions.None);
                int id = Convert.ToInt32(split[0]);
                int layer = Convert.ToInt32(split[1]);
                Node outp = new Node(id);
                outp.layer = layer;
                return outp;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return null;
            }
        }
    }
}

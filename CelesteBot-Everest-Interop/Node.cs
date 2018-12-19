using Microsoft.Xna.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CelesteBot_Everest_Interop
{
    // This class represents a Node (or a neuron)
    public class Node
    {
        public int Id; // The ID of the Node (neuron), which is ALWAYS unique
        public float InputSum = 0; // rewriten by all Nodes (neurons) that have this node (neuron) as an output. This is before activation.
        public float OutputValue = 0; // Output value to send to all Output Nodes (neurons)
        public ArrayList OutputConnections = new ArrayList(); // All of the outputs of this Node (neuron)
        public int Layer = 0; // Where is the Node (neuron)? Layer 0 = input, Layer LAST = output
        public Vector2 DrawPos = new Vector2(); // For drawing (Genome)

        public Node(int no)
        {
            // Only ID is set on construction, everything else is mutated externally as Genome sees fit
            Id = no;
        }
        // Activates the Node and relays output to future Nodes
        public void Activate()
        {
            // If not the input layer
            if (Layer != 0)
            {
                OutputValue = Sigmoid(InputSum);
            }
            // Send the outputValue * weight to each of the output Nodes of this Node
            for (int i = 0; i < OutputConnections.Count; i++)
            {
                GeneConnection temp = (GeneConnection)OutputConnections[i];
                if (temp.Enabled)
                {
                    temp.ToNode.InputSum += temp.Weight * OutputValue;
                }
                OutputConnections[i] = temp;
            }
        }
        // Simple Step
        public float StepFunction(float x)
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
        public float Relu(float x)
        {
            return Math.Max(x, 0);
        }
        // Sigmoid
        public float Sigmoid(float x)
        {
            float y = 2.0f / (1.0f + (float)Math.Pow((float)Math.E, -4.9 * x)) - 1.0f; // This is to attempt to make it so that negative and positive genes work properly!
            return y;
        }
        // Returns whether this node is connected to the parameter node
        public bool IsConnectedTo(Node node)
        {
            if (node.Layer == Layer)
            {//nodes in the same layer cannot be connected
                return false;
            }

            // If the other Node comes BEFORE this Node, check to see if this Node is an output of that Node
            if (node.Layer < Layer)
            {
                foreach (GeneConnection g in OutputConnections)
                {
                    if (g.ToNode == this)
                    {
                        return true;
                    }
                }
            }
            // Otherwise check to see if the other Node is an output of this Node
            else
            {
                foreach (GeneConnection g in OutputConnections)
                {
                    if (g.ToNode == node)
                    {
                        return true;
                    }
                }
            }
            return false;
        }
        // Clone the Node
        public Node Clone()
        {
            Node clone = new Node(Id);
            clone.Layer = Layer;
            return clone;
        }
        public override string ToString()
        {
            return "N<" + Id + ", " + Layer + ">";
        }
        public static Node NodeFromString(string str)
        {
            try
            {
                string[] split = str.Split(new string[] { "N<" }, StringSplitOptions.None)[1].Split(new string[] { ">" }, StringSplitOptions.None)[0].Split(new string[] { ", " }, StringSplitOptions.None);
                int id = Convert.ToInt32(split[0]);
                int layer = Convert.ToInt32(split[1]);
                Node outp = new Node(id);
                outp.Layer = layer;
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

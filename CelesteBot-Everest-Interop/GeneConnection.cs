using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CelesteBot_Everest_Interop
{
    // This class represents a Connection between two Nodes (refered to as a Gene)
    // This is essentially a weight.
    public class GeneConnection
    {
        public Node FromNode;
        public Node ToNode;
        public float Weight;
        public bool Enabled = true;
        public int InnovationNo; // This is essentially an ID to compare various Genomes, it is used to compare similarities and differences between Genomes.

        public GeneConnection(Node from, Node to, float w, int inno)
        {
            FromNode = from;
            ToNode = to;
            Weight = w;
            InnovationNo = inno;
        }

        // Mutates the weight randomly
        public void MutateWeight()
        {
            Random rand = new Random(Guid.NewGuid().GetHashCode());
            float rand2 = (float)rand.NextDouble();
            // Completely Re-randomize weight
            if (rand2 < CelesteBotManager.RE_RANDOMIZE_WEIGHT_CHANCE)
            {
                Weight = (float)(rand.NextDouble() - 0.5) * 2;
            }
            // Otherwise, slightly change it.
            else
            {
                Weight += RandomGaussian() / 50;
                if (Weight > 1)
                {
                    Weight = 1;
                }
                if (Weight < -1)
                {
                    Weight = -1;
                }
            }
        }

        // Gets a random number following a Gaussian Distribution
        private float RandomGaussian()
        {
            Random rand = new Random(Guid.NewGuid().GetHashCode()); //reuse this if you are generating many
            double u1 = 1.0 - rand.NextDouble(); //uniform(0,1] random doubles
            double u2 = 1.0 - rand.NextDouble();
            double randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) *
                         Math.Sin(2.0 * Math.PI * u2); //random normal(0,1)
            double randNormal = 0f + 1.0 * randStdNormal; //random normal(mean,stdDev^2)
            return (float)randNormal;
        }

        // Clones the GeneConnection
        public GeneConnection Clone(Node from, Node to)
        {
            GeneConnection clone = new GeneConnection(from, to, Weight, InnovationNo);
            clone.Enabled = Enabled;
            return clone;
        }
        public override string ToString()
        {
            // G:[N:[1,2], N:[2,2], W:3.2, I:4, E:true]
            return "G<" + FromNode + ", " + ToNode + ", W:" + Weight + ", I:" + InnovationNo + ", E:" + Enabled + ">";
        }
        public static GeneConnection GeneFromString(string str)
        {
            try
            {
                string temp = str.Split(new string[] { "G<" }, StringSplitOptions.None)[1];
                string fullstring = temp.Substring(0, temp.Length - 1);
                Node fromNode = Node.NodeFromString(fullstring.Substring(0, fullstring.IndexOf('>') + 1));
                Node toNode = Node.NodeFromString(fullstring.Substring(fullstring.IndexOf('>') + 3, fullstring.IndexOf('>', fullstring.IndexOf('>') + 3) + 1));
                string partial = fullstring.Substring(fullstring.IndexOf('>', fullstring.IndexOf('>') + 3) + 3, fullstring.Length);
                string[] split = partial.Split(new string[] { ", " }, StringSplitOptions.None);
                float weight = (float)Convert.ToDouble(split[0].Substring(2, split[0].Length));
                int innovationNo = Convert.ToInt32(split[1].Substring(2, split[1].Length));
                //bool enabled = bool.parseBoolean(split[2].Substring(2, split[2].Length-1));
                bool enabled = true;
                GeneConnection outp = new GeneConnection(fromNode, toNode, weight, innovationNo);
                outp.Enabled = enabled;
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

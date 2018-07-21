using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CelesteBot
{
    // This class represents a Connection between two Nodes (refered to as a Gene)
    // This is essentially a weight.
    public class GeneConnection
    {
        public Node fromNode;
        public Node toNode;
        public float weight;
        public bool enabled = true;
        public int innovationNo; // This is essentially an ID to compare various Genomes, it is used to compare similarities and differences between Genomes.

        public GeneConnection(Node from, Node to, float w, int inno)
        {
            fromNode = from;
            toNode = to;
            weight = w;
            innovationNo = inno;
        }

        // Mutates the weight randomly
        public void mutateWeight()
        {
            Random rand = new Random();
            float rand2 = (float)rand.NextDouble();
            // Completely Re-randomize weight
            if (rand2 < Manager.RE_RANDOMIZE_WEIGHT_CHANCE)
            {
                weight = (float)(rand.NextDouble()-0.5) * 2;
            }
            // Otherwise, slightly change it.
            else
            {
                weight += randomGaussian() / 50;
                if (weight > 1)
                {
                    weight = 1;
                }
                if (weight < -1)
                {
                    weight = -1;
                }
            }
        }

        // Gets a random number following a Gaussian Distribution
        private float randomGaussian()
        {
            Random rand = new Random(); //reuse this if you are generating many
            double u1 = 1.0 - rand.NextDouble(); //uniform(0,1] random doubles
            double u2 = 1.0 - rand.NextDouble();
            double randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) *
                         Math.Sin(2.0 * Math.PI * u2); //random normal(0,1)
            double randNormal = 0f + 1.0 * randStdNormal; //random normal(mean,stdDev^2)
            return (float)randNormal;
        }

        // Clones the GeneConnection
        public GeneConnection clone(Node from, Node to)
        {
            GeneConnection clone = new GeneConnection(from, to, weight, innovationNo);
            clone.enabled = enabled;
            return clone;
        }
        public String ToString()
        {
            // G:[N:[1,2], N:[2,2], W:3.2, I:4, E:true]
            return "G<" + fromNode + ", " + toNode + ", W:" + weight + ", I:" + innovationNo + ", E:" + enabled + ">";
        }
        public static GeneConnection geneFromString(String str)
        {
            try
            {
                String temp = str.Split(new string[]{"G<"}, StringSplitOptions.None)[1];
                String fullstring = temp.Substring(0, temp.Length - 1);
                Node fromNode = Node.nodeFromString(fullstring.Substring(0, fullstring.IndexOf('>') + 1));
                Node toNode = Node.nodeFromString(fullstring.Substring(fullstring.IndexOf('>') + 3, fullstring.IndexOf('>', fullstring.IndexOf('>') + 3) + 1));
                String partial = fullstring.Substring(fullstring.IndexOf('>', fullstring.IndexOf('>') + 3) + 3, fullstring.Length);
                String[] split = partial.Split(new string[]{", "}, StringSplitOptions.None);
                float weight = (float)Convert.ToDouble(split[0].Substring(2, split[0].Length));
                int innovationNo = Convert.ToInt32(split[1].Substring(2, split[1].Length));
                //bool enabled = bool.parseBoolean(split[2].Substring(2, split[2].Length-1));
                bool enabled = true;
                GeneConnection outp = new GeneConnection(fromNode, toNode, weight, innovationNo);
                outp.enabled = enabled;
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

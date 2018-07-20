using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CelesteBot
{
    public class ConnectionHistory
    {
        public static int nextConnectionInnovationNumber = 100;

        public int fromNode; // Start
        public int toNode; // Finish
        public int innovationNumber; // Original innovation number

        // This array is _essentially_ a Genome copy.
        // It stores all of the innovation numbers of the Genome for when the mutation first occurred.
        ArrayList originalGenomeCopy = new ArrayList();
        //the innovation Numbers from the connections of the genome which first had this mutation 
        //this represents the genome and allows us to test if another genome is the same
        //this is before this connection was added

        public ConnectionHistory(int from, int to, int inno, ArrayList innovationNos)
        {
            fromNode = from;
            toNode = to;
            innovationNumber = inno;
            originalGenomeCopy = (ArrayList)innovationNos.Clone();
        }
        // Returns whether the Genome in history matches the original Genome and the connection is between the same nodes
        public bool matches(Genome genome, Node from, Node to)
        {
            if (genome.genes.Count == originalGenomeCopy.Count)
            { // Genome+Genome Copy must have same size to match
                if (from.id == fromNode && to.id == toNode)
                { // The two Nodes in question must share the same IDs as the Nodes this History represents
                    for (int i = 0; i < genome.genes.Count; i++)
                    {
                        GeneConnection temp = (GeneConnection)(genome.genes[i]);
                        if (!originalGenomeCopy.Contains(temp.innovationNo))
                        {
                            return false; // Return false if one of the innovation numbers does not match between the Genome and the copied Genome
                        }
                    }

                    // The Genome and the original Genome match.
                    return true;
                }
            }
            return false;
        }
    }
}

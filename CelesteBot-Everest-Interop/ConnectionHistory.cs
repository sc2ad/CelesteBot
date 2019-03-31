using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;

namespace CelesteBot_Everest_Interop
{
    [KnownType(typeof(ConnectionHistory))]
    [DataContract]
    public class ConnectionHistory
    {
        [DataMember]
        public static int NextConnectionInnovationNumber = 100;

        [DataMember]
        public int FromNode; // Start
        [DataMember]
        public int ToNode; // Finish
        [DataMember]
        public int InnovationNumber; // Original innovation number

        // This array is _essentially_ a Genome copy.
        // It stores all of the innovation numbers of the Genome for when the mutation first occurred.
        [DataMember]
        ArrayList originalGenomeCopy = new ArrayList();
        //the innovation Numbers from the connections of the genome which first had this mutation 
        //this represents the genome and allows us to test if another genome is the same
        //this is before this connection was added

        public ConnectionHistory(int from, int to, int inno, ArrayList innovationNos)
        {
            FromNode = from;
            ToNode = to;
            InnovationNumber = inno;
            originalGenomeCopy = (ArrayList)innovationNos.Clone();
        }
        // Returns whether the Genome in history matches the original Genome and the connection is between the same nodes
        public bool Matches(Genome genome, Node from, Node to)
        {
            if (genome.Genes.Count == originalGenomeCopy.Count)
            { // Genome+Genome Copy must have same size to match
                if (from.Id == FromNode && to.Id == ToNode)
                { // The two Nodes in question must share the same IDs as the Nodes this History represents
                    for (int i = 0; i < genome.Genes.Count; i++)
                    {
                        GeneConnection temp = (GeneConnection)(genome.Genes[i]);
                        if (!originalGenomeCopy.Contains(temp.InnovationNo))
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

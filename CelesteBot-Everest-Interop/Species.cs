using Celeste.Mod;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;

namespace CelesteBot_Everest_Interop
{
    // This class represents a Species of Genomes (Genomes that are comparable to each other)
    [KnownType(typeof(Species))]
    [DataContract]
    public class Species
    {
        [DataMember]
        public ArrayList Players = new ArrayList();
        [DataMember]
        public float BestFitness = 0;
        [DataMember]
        public String Name;
        [DataMember]
        public CelestePlayer Champ;
        [DataMember]
        public float AverageFitness = 0;
        [DataMember]
        public int Staleness = 0;//how many generations the species has gone without an improvement
        [DataMember]
        public Genome Rep;

        // Coefficients for testing compatibility 
        [DataMember]
        float excessCoeff = 1;
        [DataMember]
        float weightDiffCoeff = 0.5f;
        [DataMember]
        float compatibilityThreshold = 3;


        public Species()
        {
            Name = CelesteBotManager.GetUniqueSpeciesName();
        }

        public Species(CelestePlayer p)
        {
            Name = CelesteBotManager.GetUniqueSpeciesName();
            p.SpeciesName = Name;
            Players.Add(p);
            // Since it is the only one in the species it is by default the best
            BestFitness = p.GetFitness();
            Rep = p.Brain.Clone();
            Champ = p.CloneForReplay();
        }

        // Returns whether the parameter Genome is in this species
        public bool SameSpecies(Genome g)
        {
            float compatibility;
            float excessAndDisjoint = GetExcessDisjoint(g, Rep);// Get the number of excess and disjoint genes between this Genome and the current species representative
            float averageWeightDiff = AverageWeightDiff(g, Rep);// Get the average weight difference between matching genes between this Genome and the current species representative

            // Makes larger Genomes slightly more compatible
            float largeGenomeNormaliser = g.Genes.Count - 20;
            if (largeGenomeNormaliser < 1)
            {
                largeGenomeNormaliser = 1;
            }

            compatibility = (excessCoeff * excessAndDisjoint / largeGenomeNormaliser) + (weightDiffCoeff * averageWeightDiff); // Compatablilty formula
            return (compatibilityThreshold > compatibility);
        }

        // Add a player to the species
        public void AddToSpecies(CelestePlayer p)
        {
            p.SpeciesName = Name;
            Players.Add(p);
        }

        // Returns the number of excess and disjoint genes between the 2 input genomes, which is the number of genes that don't match between the two Genomes
        float GetExcessDisjoint(Genome brain1, Genome brain2)
        {
            float matching = 0;
            for (int i = 0; i < brain1.Genes.Count; i++)
            {
                for (int j = 0; j < brain2.Genes.Count; j++)
                {
                    GeneConnection one = (GeneConnection)brain1.Genes[i];
                    GeneConnection two = (GeneConnection)brain2.Genes[j];
                    if (one.InnovationNo == two.InnovationNo)
                    {
                        matching++;
                        break;
                    }
                }
            }
            return (brain1.Genes.Count + brain2.Genes.Count - 2 * (matching)); // return number of excess and disjoint genes, punnett square math: pq - 2(!p!q) = nonMatching genes
        }

        // Returns the avereage weight difference between matching genes in the input genomes
        float AverageWeightDiff(Genome brain1, Genome brain2)
        {
            if (brain1.Genes.Count == 0 || brain2.Genes.Count == 0)
            {
                return 0;
            }

            float matching = 0;
            float totalDiff = 0;
            for (int i = 0; i < brain1.Genes.Count; i++)
            {
                for (int j = 0; j < brain2.Genes.Count; j++)
                {
                    GeneConnection one = (GeneConnection)brain1.Genes[i];
                    GeneConnection two = (GeneConnection)brain2.Genes[j];
                    if (one.InnovationNo == two.InnovationNo)
                    {
                        matching++;
                        totalDiff += Math.Abs(one.Weight - two.Weight);
                        break;
                    }
                }
            }
            if (matching == 0)
            {//divide by 0 error
                return 1000; // Return some large number because none of the genes matched
            }
            return totalDiff / matching;
        }

        // Sorts the species by fitness 
        public void SortSpecies()
        {
            ArrayList temp = new ArrayList();

            // Selection sort (WILL REPLACE WITH QUICKSORT)
            // temp = Util.sort(temp);
            for (int i = 0; i < Players.Count; i++)
            {
                float max = 0;
                int maxIndex = 0;
                for (int j = 0; j < Players.Count; j++)
                {
                    CelestePlayer p = (CelestePlayer)Players[j];
                    if (p.GetFitness() > max)
                    {
                        max = p.GetFitness();
                        maxIndex = j;
                    }
                }
                temp.Add(Players[maxIndex]);
                Players.RemoveAt(maxIndex);
                i--;
            }

            Players = (ArrayList)temp.Clone();
            if (Players.Count == 0)
            {
                //print("uhoh, no players!");
                Staleness = 200;
                return;
            }
            // If new best player
            CelestePlayer first = (CelestePlayer)Players[0];
            if (first.GetFitness() > BestFitness)
            {
                Staleness = 0;
                BestFitness = first.GetFitness();
                Rep = first.Brain.Clone();
                Champ = first.CloneForReplay();
            }
            else
            { // If no new best player
                Staleness++;
            }
        }

        // Sets the average fitness for the Species
        public void SetAverage()
        {

            float sum = 0;
            for (int i = 0; i < Players.Count; i++)
            {
                CelestePlayer temp = (CelestePlayer)Players[i];
                sum += temp.GetFitness();
            }
            AverageFitness = sum / Players.Count;
        }

        // Gets the offspring from the CelestePlayer in this species
        public CelestePlayer GetOffspring(ArrayList innovationHistory)
        {
            CelestePlayer baby;
            Random rand = new Random(Guid.NewGuid().GetHashCode());

            if (rand.NextDouble() < 0.25)
            {// Punnett square math: 25% of the time there is no crossover and the child is simply a clone of a random(ish) player
                baby = SelectPlayer().Clone();
            }
            else
            {// Punnet square math: 75% of the time do crossover 

                // Get 2 random parents 
                CelestePlayer parent1 = SelectPlayer();
                CelestePlayer parent2 = SelectPlayer();

                // The crossover function expects the highest fitness parent to be the object and the lowest as the argument
                if (parent1.GetFitness() < parent2.GetFitness())
                {
                    baby = parent2.Crossover(parent1);
                }
                else
                {
                    baby = parent1.Crossover(parent2);
                }
            }
            baby.Brain.Mutate(innovationHistory);// Mutate offspring brain
            Logger.Log(CelesteBotInteropModule.ModLogKey, "Species: "+Name+" has a new mutation of Baby when getting offspring");
            return baby;
        }

        // Selects a player based on it fitness.
        // Uses a running sum probability
        CelestePlayer SelectPlayer()
        {
            float fitnessSum = 0;
            for (int i = 0; i < Players.Count; i++)
            {
                CelestePlayer p = (CelestePlayer)Players[i];
                fitnessSum += p.GetFitness();
            }
            Random r = new Random(Guid.NewGuid().GetHashCode());
            float rand = (float)(r.NextDouble() * (fitnessSum));
            float runningSum = 0;

            for (int i = 0; i < Players.Count; i++)
            {
                CelestePlayer p = (CelestePlayer)Players[i];
                runningSum += p.GetFitness();
                if (runningSum > rand)
                {
                    return p;
                }
            }
            return (CelestePlayer)Players[0];
        }

        // Kills off bottom half of the species
        public void Cull()
        {
            if (Players.Count > 2)
            {
                for (int i = Players.Count / 2; i < Players.Count; i++)
                {
                    CelestePlayer temp = (CelestePlayer)Players[i];
                    Players.RemoveAt(i);
                    i--;
                    temp.Dispose();
                }
            }
        }

        // In order to protect unique Players, the fitnesses of each CelestePlayer is divided by the number of Players in its Species
        // Makes larger Species have lower fitness
        public void FitnessSharing()
        {
            for (int i = 0; i < Players.Count; i++)
            {
                CelestePlayer p = (CelestePlayer)Players[i];
                p.Fitness /= Players.Count;
            }
        }
    }
}

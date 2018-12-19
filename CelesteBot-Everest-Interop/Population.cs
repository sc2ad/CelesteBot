using Celeste.Mod;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CelesteBot_Everest_Interop
{
    // This class represents ALL of the Players that will be evolved.
    // It serves as a 'holder' for all of the Players of each generation and is also responsible for evolving them+showing+updating them
    [Serializable]
    public class Population
    {
        public ArrayList Pop = new ArrayList();
        public CelestePlayer BestPlayer;// The best player in the population 
        public float BestFitness = 0;// The score of the best ever player
        public int Gen;
        public ArrayList InnovationHistory = new ArrayList();
        public ArrayList GenPlayers = new ArrayList();
        public ArrayList Species = new ArrayList();

        bool massExtinctionEvent = false;
        bool newStage = false;

        public int CurrentIndex = 0;

        public Population(int size)
        {
            for (int i = 0; i < size; i++)
            {
                CelestePlayer toAdd = new CelestePlayer();
                toAdd.Brain.GenerateNetwork();
                toAdd.Brain.Mutate(InnovationHistory);
                Pop.Add(toAdd);
            }
            CurrentIndex = 0;
        }

        public Population()
        {
            for (int i = 0; i < CelesteBotInteropModule.Settings.OrganismsPerGeneration; i++)
            {
                CelestePlayer toAdd = new CelestePlayer();
                toAdd.Brain.GenerateNetwork();
                toAdd.Brain.Mutate(InnovationHistory);
                Pop.Add(toAdd);
            }
            CurrentIndex = 0;
        }

        // Update all the players which are alive
        public void UpdateAlive()
        {
            if (CurrentIndex < Pop.Count)
            {
                CelestePlayer p = (CelestePlayer)Pop[CurrentIndex];
                p.Update();
            }
        }

        // Gets the current player if updating with UpdateType.SINGLE
        public CelestePlayer GetCurrentPlayer()
        {
            if (CurrentIndex < Pop.Count)
            {
                return (CelestePlayer)Pop[CurrentIndex];
            }
            return null; // don't do this if you aren't single updating
        }

        // Returns true if all the players are dead, how sad
        public bool Done()
        {
            for (int i = 0; i < Pop.Count; i++)
            {
                CelestePlayer temp = (CelestePlayer)Pop[i];
                if (!temp.Dead)
                {
                    return false;
                }
            }
            Logger.Log(CelesteBotInteropModule.ModLogKey, "all dead");
            return true;
        }

        // Sets the best player globally and for this gen
        public void SetBestPlayer()
        {
            Species s = (Species)Species[0];
            CelestePlayer tempBest = (CelestePlayer)s.Players[0];
            tempBest.Gen = Gen;

            // If the best CelestePlayer this gen is better than the global best fitness then set the global best to this CelestePlayer
            if (tempBest.Fitness >= BestFitness)
            {
                GenPlayers.Add(tempBest.CloneForReplay());
                Logger.Log(CelesteBotInteropModule.ModLogKey,"old best: "+ BestFitness);
                Logger.Log(CelesteBotInteropModule.ModLogKey, "new best: "+ tempBest.Fitness);
                BestFitness = tempBest.Fitness;
                BestPlayer = tempBest.CloneForReplay();
            }
        }

        // This function is called when all the players in the population are dead and a new generation needs to be made
        public void NaturalSelection()
        {
            Speciate();//seperate the population into species 
            CalculateFitness();//calculate the fitness of each player
            SortSpecies();//sort the species to be ranked in fitness order, best first
            if (massExtinctionEvent)
            {
                MassExtinction();
                massExtinctionEvent = false;
                Logger.Log(CelesteBotInteropModule.ModLogKey, "MASS EXTINCTION!");
            }
            CullSpecies();//kill off the bottom half of each species
            SetBestPlayer();//save the best player of this gen
            KillStaleSpecies();//remove species which haven't improved in the last 15(ish) generations
            KillBadSpecies();//kill species which are so bad that they cant reproduce


            Logger.Log(CelesteBotInteropModule.ModLogKey, "generation: "+ Gen + " Number of mutations: " + InnovationHistory.Count + " species: " + Species.Count);

            float averageSum = GetAvgFitnessSum();
            ArrayList children = new ArrayList();//the next generation
            //println("Species:");
            for (int j = 0; j < Species.Count; j++)
            {//for each species
                Species s = (Species)Species[j];
                Logger.Log(CelesteBotInteropModule.ModLogKey, "Species: " + j + " with Name: " + s.Name + " has best fitness: " + s.BestFitness);
                string playerStr = "";
                for (int i = 0; i < s.Players.Count; i++)
                {
                    CelestePlayer p = (CelestePlayer)s.Players[i];
                    playerStr += "Player: " + i + ": " + p.Name + " with fitness: " + p.Fitness+", ";
                }
                Logger.Log(CelesteBotInteropModule.ModLogKey, "With Players: "+playerStr);

                CelestePlayer c = s.Champ.CloneForReplay(); // used to be cloneForReplay
                c.SpeciesName = s.Name;
                children.Add(c);//add champion without any mutation

                int NoOfChildren = (int)Math.Floor(s.AverageFitness / averageSum * Pop.Count) - 1;//the number of children this species is allowed, note -1 is because the champ is already added
                for (int i = 0; i < NoOfChildren; i++)
                {//get the calculated amount of children from this species
                    CelestePlayer temp = s.GetOffspring(InnovationHistory);
                    temp.SpeciesName = s.Name;
                    temp.Gen = Gen;
                    children.Add(temp);
                }
            }

            while (children.Count < Pop.Count)
            {//if not enough babies (due to flooring the number of children to get a whole int)
                Species best = (Species)Species[0];
                CelestePlayer temp = best.GetOffspring(InnovationHistory);
                temp.SpeciesName = best.Name;
                temp.Gen = Gen;
                children.Add(temp);//get babies from the best species
            }
            Pop.Clear();
            Pop = (ArrayList)children.Clone(); //set the children as the current population
            Gen += 1;
            foreach (CelestePlayer p in Pop)
            {//generate networks for each of the children
                p.Brain.GenerateNetwork();
            }
            CurrentIndex = 0;
        }

        // Seperate population into species based on how similar they are to the representatives of each species in the previous gen
        public void Speciate()
        {
            foreach (Species s in Species)
            {//empty species
                s.Players.Clear();
            }
            foreach (CelestePlayer p in Pop)
            {//for each player
                bool speciesFound = false;
                foreach (Species s in Species)
                {//for each species
                    if (s.SameSpecies(p.Brain))
                    {//if the player is similar enough to be considered in the same species
                        s.AddToSpecies(p);//add it to the species
                        speciesFound = true;
                        break;
                    }
                }
                if (!speciesFound)
                {// If no species was similar enough then add a new species with this as its representative
                    Species.Add(new Species(p));
                }
            }
        }

        // Calculates the fitness of all of the players (except the first one)
        public void CalculateFitness()
        {
            for (int i = 1; i < Pop.Count; i++)
            {
                CelestePlayer p = (CelestePlayer)Pop[i];
                p.CalculateFitness();
            }
        }
        // Sorts the players within a species and the species by their fitnesses
        public void SortSpecies()
        {
            //sort the players within a species
            foreach (Species s in Species)
            {
                s.SortSpecies();
            }

            //sort the species by the fitness of its best player
            //using selection sort like a loser
            // Util.sort(species)
            ArrayList temp = new ArrayList();
            for (int i = 0; i < Species.Count; i++)
            {
                float max = 0;
                int maxIndex = 0;
                for (int j = 0; j < Species.Count; j++)
                {
                    Species s = (Species)Species[j];
                    if (s.BestFitness > max)
                    {
                        max = s.BestFitness;
                        maxIndex = j;
                    }
                }
                temp.Add((Species)Species[maxIndex]);
                Species.RemoveAt(maxIndex);
                i--;
            }
            Species = (ArrayList)temp.Clone();
        }

        // Kills all species which haven't improved in 15 generations
        public void KillStaleSpecies()
        {
            for (int i = 2; i < Species.Count; i++)
            {
                Species s = (Species)Species[i];
                if (s.Staleness >= 15)
                {
                    Species.RemoveAt(i);
                    i--;
                }
            }
        }

        // If a species sucks so much that it wont even be allocated 1 child for the next generation then kill it now
        public void KillBadSpecies()
        {
            float averageSum = GetAvgFitnessSum();

            for (int i = 1; i < Species.Count; i++)
            {
                Species s = (Species)Species[i];
                if (s.AverageFitness / averageSum * s.Players.Count < 1)
                {//if wont be given a single child 
                    Species.RemoveAt(i);//sad
                    i--;
                }
            }
        }

        // Returns the sum of each species' average fitness
        public float GetAvgFitnessSum()
        {
            float averageSum = 0;
            foreach (Species s in Species)
            {
                averageSum += s.AverageFitness;
            }
            return averageSum;
        }

        // Kill the bottom half of each species
        public void CullSpecies()
        {
            foreach (Species s in Species)
            {
                s.Cull(); //kill bottom half
                s.FitnessSharing();//also while we're at it lets do fitness sharing
                s.SetAverage();//reset averages because they will have changed
            }
        }

        // Kill all species that aren't in the top
        public void MassExtinction()
        {
            for (int i = CelesteBotManager.EXTINCTION_SAVE_TOP; i < Species.Count; i++)
            {
                Species.RemoveAt(i);//sad
                i--;
            }
        }
        // Need to add checkpointing here.
    }
}

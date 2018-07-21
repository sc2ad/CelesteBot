using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CelesteBot
{
    class CelestePlayer
    {
        float fitness = -1; // Default so that it is known when fitness isn't calculated
        float unadjustedFitness;
        Genome brain;
        ArrayList replayActions = new ArrayList();
        float[] vision = new float[Manager.INPUTS];
        float[] actions = new float[Manager.OUTPUTS];
        int lifespan = 0;
        int bestScore = 0;
        bool dead = false;
        bool replay = false;
        int gen = 0;
        int score = 0;
        String name;
        String speciesName = "Not yet defined";

        float x, y;
        float vx, vy;

        public CelestePlayer()
        {
            brain = new Genome(Manager.INPUTS, Manager.OUTPUTS);
            name = Manager.GetUniqueOrganismName();
        }
        void move()
        {
            // Need to output controller inputs here.
            // The controller then does the actions to 'move' the player.
            lifespan++;
            
        }
        void update()
        {
            // Updates sprite
            move();
            // Also calculate live fitness here. The fitness should weight distance to goal, then time it takes to get there.
            // It might also be easier to apply some sort of fitness based off of velocity of character (that way the faster it goes, the higher the fitness it has)
            // But make sure that making it to the end is still highest priority.

            // Also update the various parameters the player has here.
            // Ex: Player x, Player y, Player vx, Player vy, vision (?)
        }
        void look()
        {
            // Updates vision array with proper values each frame
            // LAST STEP!
            /*
            Inputs: PlayerX, PlayerY, PlayerXSpeed, PlayerYSpeed, <INPUTS FROM VISUALIZATION OF GAME>
            IT IS ALSO POSSIBLE THAT X AND Y ARE UNNEEDED, AS THE VISUALIZATION INPUTS MAY BE ENOUGH
            Outputs: U, D, L, R, Jump, Dash, Climb
            If any of the outputs are above 0.7, apply them when returning controller output
            */
            vision[0] = (float)x / ((float)width); // Normalize x and y to the width and height of the scene
            vision[1] = (float)y / ((float)height);
            vision[2] = Math.Abs(vx); // might not want abs?
            vision[3] = vy;
        }
        // Updates controller inputs based on neural network output
        void think()
        {
            float max = 0;
            int maxIndex = 0;
            //get the output of the neural network
            actions = brain.feedForward(vision);
            if (replay)
            {
                //println(vision);
                for (int i = 0; i < actions.length; i++)
                {
                    if (lifespan >= replayActions.size())
                    {
                        dead = true;
                        return;
                    }
                    actions[i] = replayActions.get(lifespan)[i];
                }
            }

            float[] temp = new float[actions.length];
            for (int i = 0; i < actions.length; i++)
            {
                if (actions[i] > max)
                {
                    max = actions[i];
                    maxIndex = i;
                }
                temp[i] = actions[i];
            }
            if (!replay)
            {
                replayActions.add(temp);
            }

            // Need to convert actions float values into controller inputs here.
            // Then needs to return controller inputs so that the player can move
        }
        // Clones CelestePlayer
        CelestePlayer clone()
        {
            CelestePlayer outp = new CelestePlayer();
            outp.replay = false;
            outp.fitness = fitness;
            outp.gen = gen;
            outp.bestScore = score;
            outp.brain = brain.clone();
            return outp;
        }
        // Clones for replaying
        CelestePlayer cloneForReplay()
        {
            CelestePlayer outp = new CelestePlayer();
            outp.replaySpikes = (ArrayList)replaySpikes.clone();
            outp.replayActions = (ArrayList)replayActions.clone();
            outp.replay = true;
            outp.fitness = fitness;
            outp.gen = gen;
            outp.bestScore = score;
            outp.brain = brain.clone();
            outp.name = name;
            outp.speciesName = speciesName;
            outp.score = 0;
            return outp;
        }
        // Calculates fitness
        void calculateFitness()
        {
            fitness = (2 * score) * (2 * score) + lifespan;
            // MODIFY!
        }
        // Getter method for fitness (rarely used)
        float getFitness()
        {
            if (fitness < 0)
            {
                calculateFitness();
            }
            return fitness;
        }
        // Crossover function - less fit parent is parent2
        CelestePlayer crossover(CelestePlayer parent2)
        {
            CelestePlayer child = new CelestePlayer();

            child.brain = brain.crossover(parent2.brain);
            child.brain.generateNetwork();

            return child;
        }
        public override string ToString()
        {
            string outp = "P<Name:" + name;
            outp += ", speciesName:" + speciesName;
            outp += ", gen:" + gen;
            outp += ", fitness:" + fitness;
            outp += ", bestScore:" + bestScore;
            outp += ", replay:" + replay;
            outp += ", ACTIONS:`";
            foreach (float[] f in replayActions)
            {
                outp += "<";
                for (int i = 0; i < f.length; i++)
                {
                    outp += f[i] + ", ";
                }
              outp = outp.substring(0, outp.length() - 2); // Removes ", " at end
              outp += ">, ";
            }
            outp = outp.substring(0, outp.length() - 2); // Removes ", " at end
            outp += ",, SPIKES:" + spikesToString(replaySpikes);
            outp += ", " + brain + ">";
            return outp;
        }
        public static CelestePlayer playerFromString(String str)
        {
            try
            {
                str = str.split("P<")[1];
                String name = str.split("Name:")[1].split(", ")[0];
                String speciesName = str.split("speciesName:")[1].split(", ")[0];
                int gen = Integer.parseInt(str.split("gen:")[1].split(", ")[0]);
                float fitness = float.parseFloat(str.split("fitness:")[1].split(", ")[0]);
                int bestScore = Integer.parseInt(str.split("bestScore:")[1].split(", ")[0]);
                bool replay = bool.parseBoolean(str.split("replay:")[1].split(", ")[0]);
                String forActions = str.split(", ACTIONS:`")[1].split(", SPIKES")[0];
                ArrayList<float[]> replayActions = new ArrayList<float[]>();
                while (forActions.contains(">"))
                {
                    String singleton = forActions.split("<")[1].split(">")[0];
                    String[] arr = singleton.split(", "); // Gets all of the floats individually
                    if (arr.length == 0)
                    {
                        arr = new String[] { singleton };
                    }
                    int len = arr.length;
                    float[] floats = new float[len];
                    for (int i = 0; i < len; i++)
                    {
                        floats[i] = float.parseFloat(arr[i]);
                    }
                    replayActions.add(floats);
                    forActions = forActions.substring(forActions.indexOf(">") + 1, forActions.length());
                }
                String forSpikes = str.split("SPIKES:")[1];
                ArrayList<Spike[]> replaySpikes = spikesFromString(forSpikes);
                Genome brain = genomeFromString(forSpikes);
                CelestePlayer outp = new CelestePlayer();
                outp.replaySpikes = (ArrayList)replaySpikes.clone();
                outp.replayActions = (ArrayList)replayActions.clone();
                outp.replay = false;
                outp.fitness = fitness;
                outp.gen = gen;
                outp.bestScore = bestScore;
                outp.brain = brain.clone();
                outp.name = name;
                outp.speciesName = speciesName;
                return outp;
            }
            catch (Exception e)
            {
                e.printStackTrace();
                return null;
            }
        }
    }

   
}

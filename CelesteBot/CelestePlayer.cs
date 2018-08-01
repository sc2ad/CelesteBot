//using System;
//using System.Collections;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace CelesteBot
//{
//    class CelestePlayer
//    {
//        float fitness = -1; // Default so that it is known when fitness isn't calculated
//        float unadjustedFitness;
//        Genome brain;
//        ArrayList replayActions = new ArrayList();
//        float[] vision = new float[Manager.INPUTS];
//        float[] actions = new float[Manager.OUTPUTS];
//        int lifespan = 0;
//        bool dead = false;
//        bool replay = false;
//        int gen = 0;
//        String name;
//        String speciesName = "Not yet defined";

//        public float Fitness { get => fitness; set => fitness = value; }
//        public float UnadjustedFitness { get => unadjustedFitness; set => unadjustedFitness = value; }
//        public Genome Brain { get => brain; set => brain = value; }
//        public ArrayList ReplayActions { get => replayActions; set => replayActions = value; }
//        public float[] Vision { get => vision; set => vision = value; }
//        public float[] Actions { get => actions; set => actions = value; }
//        public int Lifespan { get => lifespan; set => lifespan = value; }
//        public bool Dead { get => dead; set => dead = value; }
//        public bool Replay { get => replay; set => replay = value; }
//        public int Gen { get => gen; set => gen = value; }
//        public string Name { get => name; set => name = value; }
//        public string SpeciesName { get => speciesName; set => speciesName = value; }

//        public CelestePlayer()
//        {
//            Brain = new Genome(Manager.INPUTS, Manager.OUTPUTS);
//            Name = Manager.GetUniqueOrganismName();
//        }
//        void move()
//        {
//            // Need to output controller inputs here.
//            // The controller then does the actions to 'move' the player.
//            Lifespan++;
            
//        }
//        void update()
//        {
//            // Updates sprite
//            move();
//            // Also calculate live fitness here. The fitness should weight distance to goal, then time it takes to get there.
//            // It might also be easier to apply some sort of fitness based off of velocity of character (that way the faster it goes, the higher the fitness it has)
//            // But make sure that making it to the end is still highest priority.

//            // Also update the various parameters the player has here.
//            // Ex: Player x, Player y, Player vx, Player vy, vision (?)
//        }
//        void look()
//        {
//            // Updates vision array with proper values each frame
//            // LAST STEP!
//            /*
//            Inputs: PlayerX, PlayerY, PlayerXSpeed, PlayerYSpeed, <INPUTS FROM VISUALIZATION OF GAME>
//            IT IS ALSO POSSIBLE THAT X AND Y ARE UNNEEDED, AS THE VISUALIZATION INPUTS MAY BE ENOUGH
//            Outputs: U, D, L, R, Jump, Dash, Climb
//            If any of the outputs are above 0.7, apply them when returning controller output
//            */
//            Vision[0] = (float)x / ((float)width); // Normalize x and y to the width and height of the scene
//            Vision[1] = (float)y / ((float)height);
//            Vision[2] = Math.Abs(vx); // might not want abs?
//            Vision[3] = vy;
//        }
//        // Updates controller inputs based on neural network output
//        void think()
//        {
//            float max = 0;
//            int maxIndex = 0;
//            //get the output of the neural network
//            Actions = Brain.feedForward(Vision);
//            if (Replay)
//            {
//                //println(vision);
//                for (int i = 0; i < Actions.length; i++)
//                {
//                    if (Lifespan >= ReplayActions.size())
//                    {
//                        Dead = true;
//                        return;
//                    }
//                    Actions[i] = ReplayActions.get(Lifespan)[i];
//                }
//            }

//            float[] temp = new float[Actions.length];
//            for (int i = 0; i < Actions.length; i++)
//            {
//                if (Actions[i] > max)
//                {
//                    max = Actions[i];
//                    maxIndex = i;
//                }
//                temp[i] = Actions[i];
//            }
//            if (!Replay)
//            {
//                ReplayActions.Add(temp);
//            }

//            // Need to convert actions float values into controller inputs here.
//            // Then needs to return controller inputs so that the player can move
//        }
//        // Clones CelestePlayer
//        CelestePlayer clone()
//        {
//            CelestePlayer outp = new CelestePlayer();
//            outp.Replay = false;
//            outp.Fitness = Fitness;
//            outp.Gen = Gen;
//            outp.bestScore = score;
//            outp.Brain = Brain.clone();
//            return outp;
//        }
//        // Clones for replaying
//        CelestePlayer cloneForReplay()
//        {
//            CelestePlayer outp = new CelestePlayer();
//            outp.replaySpikes = (ArrayList)replaySpikes.clone();
//            outp.ReplayActions = (ArrayList)ReplayActions.clone();
//            outp.Replay = true;
//            outp.Fitness = Fitness;
//            outp.Gen = Gen;
//            outp.bestScore = score;
//            outp.Brain = Brain.clone();
//            outp.Name = Name;
//            outp.SpeciesName = SpeciesName;
//            outp.score = 0;
//            return outp;
//        }
//        // Calculates fitness
//        void calculateFitness()
//        {
//            Fitness = (2 * score) * (2 * score) + Lifespan;
//            // MODIFY!
//        }
//        // Getter method for fitness (rarely used)
//        float getFitness()
//        {
//            if (Fitness < 0)
//            {
//                calculateFitness();
//            }
//            return Fitness;
//        }
//        // Crossover function - less fit parent is parent2
//        CelestePlayer crossover(CelestePlayer parent2)
//        {
//            CelestePlayer child = new CelestePlayer();

//            child.Brain = Brain.crossover(parent2.Brain);
//            child.Brain.generateNetwork();

//            return child;
//        }
//        public override string ToString()
//        {
//            string outp = "P<Name:" + Name;
//            outp += ", speciesName:" + SpeciesName;
//            outp += ", gen:" + Gen;
//            outp += ", fitness:" + Fitness;
//            outp += ", bestScore:" + bestScore;
//            outp += ", replay:" + Replay;
//            outp += ", ACTIONS:`";
//            foreach (float[] f in ReplayActions)
//            {
//                outp += "<";
//                for (int i = 0; i < f.length; i++)
//                {
//                    outp += f[i] + ", ";
//                }
//              outp = outp.substring(0, outp.length() - 2); // Removes ", " at end
//              outp += ">, ";
//            }
//            outp = outp.substring(0, outp.length() - 2); // Removes ", " at end
//            outp += ",, SPIKES:" + spikesToString(replaySpikes);
//            outp += ", " + Brain + ">";
//            return outp;
//        }
//        public static CelestePlayer playerFromString(String str)
//        {
//            try
//            {
//                str = str.split("P<")[1];
//                String name = str.split("Name:")[1].split(", ")[0];
//                String speciesName = str.split("speciesName:")[1].split(", ")[0];
//                int gen = Integer.parseInt(str.split("gen:")[1].split(", ")[0]);
//                float fitness = float.parseFloat(str.split("fitness:")[1].split(", ")[0]);
//                int bestScore = Integer.parseInt(str.split("bestScore:")[1].split(", ")[0]);
//                bool replay = bool.parseBoolean(str.split("replay:")[1].split(", ")[0]);
//                String forActions = str.split(", ACTIONS:`")[1].split(", SPIKES")[0];
//                ArrayList<float[]> replayActions = new ArrayList<float[]>();
//                while (forActions.contains(">"))
//                {
//                    String singleton = forActions.split("<")[1].split(">")[0];
//                    String[] arr = singleton.split(", "); // Gets all of the floats individually
//                    if (arr.length == 0)
//                    {
//                        arr = new String[] { singleton };
//                    }
//                    int len = arr.length;
//                    float[] floats = new float[len];
//                    for (int i = 0; i < len; i++)
//                    {
//                        floats[i] = float.parseFloat(arr[i]);
//                    }
//                    replayActions.add(floats);
//                    forActions = forActions.substring(forActions.indexOf(">") + 1, forActions.length());
//                }
//                String forSpikes = str.split("SPIKES:")[1];
//                ArrayList<Spike[]> replaySpikes = spikesFromString(forSpikes);
//                Genome brain = genomeFromString(forSpikes);
//                CelestePlayer outp = new CelestePlayer();
//                outp.replaySpikes = (ArrayList)replaySpikes.clone();
//                outp.ReplayActions = (ArrayList)replayActions.clone();
//                outp.Replay = false;
//                outp.Fitness = fitness;
//                outp.Gen = gen;
//                outp.bestScore = bestScore;
//                outp.Brain = brain.clone();
//                outp.Name = name;
//                outp.SpeciesName = speciesName;
//                return outp;
//            }
//            catch (Exception e)
//            {
//                e.printStackTrace();
//                return null;
//            }
//        }
//    }

   
//}
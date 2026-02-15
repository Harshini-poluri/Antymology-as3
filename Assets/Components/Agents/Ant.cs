using System.Collections.Generic;
using UnityEngine;
using Antymology.Terrain;

namespace Antymology.Agents
{
    // this is the main ant class, each ant in the simulation is one of these
    public class Ant : MonoBehaviour
    {
        // how much health the ant currently has
        public float currentHealth;

        // the max health the ant can have
        public float maximumHealth;

        // true if this ant is the queen, false if its a worker
        public bool isThisTheQueen;

        // keeps track of if the ant is dead or alive
        public bool isThisAntDead;

        // the x position of the ant in the world grid
        public int positionX;

        // the y position of the ant in the world grid (up and down)
        public int positionY;

        // the z position of the ant in the world grid
        public int positionZ;

        // the genome that controls this ant's brain
        public AntGenome myGenome;

        // the neural network that decides what the ant does
        private NeuralNetwork myBrain;

        // reference to the manager that controls the whole colony
        private AntColonyManager theColonyManager;

        // all the different things an ant can do
        public enum AntAction
        {
            MoveNorth = 0,
            MoveSouth = 1,
            MoveEast = 2,
            MoveWest = 3,
            Dig = 4,
            EatMulch = 5,
            PlaceNest = 6,
            ShareHealth = 7,
            DoNothing = 8
        }

        // this sets up the ant with everything it needs when it first spawns
        public void Setup(AntGenome inputGenome, bool queenOrNot, int startingX, int startingY, int startingZ, AntColonyManager managerReference)
        {
            myGenome = inputGenome;
            isThisTheQueen = queenOrNot;
            theColonyManager = managerReference;

            // set health to max at the start
            maximumHealth = ConfigurationManager.Instance.AntMaxHealth;
            currentHealth = maximumHealth;
            isThisAntDead = false;

            // set the starting position
            positionX = startingX;
            positionY = startingY;
            positionZ = startingZ;

            // make the neural network and load in the genome weights
            myBrain = new NeuralNetwork(
                ConfigurationManager.Instance.NeuralNetInputSize,
                ConfigurationManager.Instance.NeuralNetHiddenSize,
                ConfigurationManager.Instance.NeuralNetOutputSize
            );
            myBrain.LoadWeightsFromGenome(inputGenome);

            // move the ant object to where it should be in the world
            MoveAntObjectToGridPosition();
        }

        // this runs every simulation tick, it drains health, checks if dead, then picks an action
        public void DoOneStep()
        {
            if (isThisAntDead) return;

            // first make sure ant is standing on something solid, not floating in air
            AbstractBlock blockUnderAnt = WorldManager.Instance.GetBlock(positionX, positionY, positionZ);
            if (blockUnderAnt is AirBlock)
            {
                // fell through! find the ground
                positionY = FindTopBlockY(positionX, positionZ);
            }

            // figure out how much health to drain this step
            float healthLostThisStep = ConfigurationManager.Instance.HealthDrainPerStep;

            // check if standing on acid - if so health drains twice as fast
            blockUnderAnt = WorldManager.Instance.GetBlock(positionX, positionY, positionZ);
            if (blockUnderAnt is AcidicBlock)
            {
                healthLostThisStep = healthLostThisStep * 2f;
            }

            // take away health
            currentHealth = currentHealth - healthLostThisStep;

            // if health hit zero or below, the ant dies
            if (currentHealth <= 0f)
            {
                KillThisAnt();
                return;
            }

            // get the inputs for the neural network (what the ant can see around it)
            float[] whatTheAntSees = GetNeuralNetworkInputs();

            // run the neural network to get what the ant wants to do
            float[] actionScores = myBrain.RunForward(whatTheAntSees);

            // try to do the action with the highest score, if it cant do that try the next one
            TryToDoTheBestAction(actionScores);

            // update where the ant appears in the 3d world
            MoveAntObjectToGridPosition();
        }

        // this gets all the info the ant can "see" and puts it into numbers for the neural network
        private float[] GetNeuralNetworkInputs()
        {
            float[] allInputs = new float[ConfigurationManager.Instance.NeuralNetInputSize];
            int inputIndex = 0;

            // input 0: how much health does the ant have (0 means empty, 1 means full)
            allInputs[inputIndex] = currentHealth / maximumHealth;
            inputIndex++;

            // input 1: is this ant the queen? 1 for yes, 0 for no
            allInputs[inputIndex] = isThisTheQueen ? 1f : 0f;
            inputIndex++;

            // input 2: is the ant standing on mulch? (food)
            AbstractBlock blockHere = WorldManager.Instance.GetBlock(positionX, positionY, positionZ);
            allInputs[inputIndex] = (blockHere is MulchBlock) ? 1f : 0f;
            inputIndex++;

            // input 3: is the ant standing on acid?
            allInputs[inputIndex] = (blockHere is AcidicBlock) ? 1f : 0f;
            inputIndex++;

            // these arrays tell us which direction north south east west are
            int[] xDirections = { 0, 0, 1, -1 };
            int[] zDirections = { 1, -1, 0, 0 };

            // inputs 4-7: what type of block is in each direction
            for (int dir = 0; dir < 4; dir++)
            {
                int checkX = positionX + xDirections[dir];
                int checkZ = positionZ + zDirections[dir];
                int topY = FindTopBlockY(checkX, checkZ);
                AbstractBlock neighborBlock = WorldManager.Instance.GetBlock(checkX, topY, checkZ);
                allInputs[inputIndex] = TurnBlockTypeIntoNumber(neighborBlock);
                inputIndex++;
            }

            // inputs 8-11: how much higher or lower is the ground in each direction
            for (int dir = 0; dir < 4; dir++)
            {
                int checkX = positionX + xDirections[dir];
                int checkZ = positionZ + zDirections[dir];
                int topY = FindTopBlockY(checkX, checkZ);
                float howMuchHigher = (topY - positionY) / 5f;
                allInputs[inputIndex] = Mathf.Clamp(howMuchHigher, -1f, 1f);
                inputIndex++;
            }

            // inputs 12-15: how strong is the pheromone smell in each direction
            for (int dir = 0; dir < 4; dir++)
            {
                int checkX = positionX + xDirections[dir];
                int checkZ = positionZ + zDirections[dir];
                allInputs[inputIndex] = theColonyManager.GetPheromoneStrength(checkX, checkZ);
                inputIndex++;
            }

            // inputs 16-17: which direction is the queen (only matters for workers)
            if (!isThisTheQueen && theColonyManager.theQueen != null && !theColonyManager.theQueen.isThisAntDead)
            {
                float directionToQueenX = theColonyManager.theQueen.positionX - positionX;
                float directionToQueenZ = theColonyManager.theQueen.positionZ - positionZ;
                float distanceToQueen = Mathf.Sqrt(directionToQueenX * directionToQueenX + directionToQueenZ * directionToQueenZ);
                if (distanceToQueen > 0.01f)
                {
                    allInputs[inputIndex] = Mathf.Clamp(directionToQueenX / distanceToQueen, -1f, 1f);
                    inputIndex++;
                    allInputs[inputIndex] = Mathf.Clamp(directionToQueenZ / distanceToQueen, -1f, 1f);
                    inputIndex++;
                }
                else
                {
                    allInputs[inputIndex] = 0f;
                    inputIndex++;
                    allInputs[inputIndex] = 0f;
                    inputIndex++;
                }
            }
            else
            {
                allInputs[inputIndex] = 0f;
                inputIndex++;
                allInputs[inputIndex] = 0f;
                inputIndex++;
            }

            return allInputs;
        }

        // turns a block type into a number between 0 and 1 so the neural net can understand it
        private float TurnBlockTypeIntoNumber(AbstractBlock theBlock)
        {
            if (theBlock is AirBlock) return 0f;
            if (theBlock is GrassBlock) return 0.17f;
            if (theBlock is StoneBlock) return 0.33f;
            if (theBlock is MulchBlock) return 0.5f;
            if (theBlock is NestBlock) return 0.67f;
            if (theBlock is AcidicBlock) return 0.83f;
            if (theBlock is ContainerBlock) return 1f;
            return 0f;
        }

        // sorts the neural network outputs and tries to do the best action first
        // if that action is invalid, it tries the next best one, and so on
        private void TryToDoTheBestAction(float[] actionScores)
        {
            // make a list of action indexes sorted by their scores (highest first)
            List<int> sortedActionIndexes = new List<int>();
            for (int i = 0; i < actionScores.Length; i++)
                sortedActionIndexes.Add(i);
            sortedActionIndexes.Sort((a, b) => actionScores[b].CompareTo(actionScores[a]));

            // go through each action and try it, stop when one works
            foreach (int actionIndex in sortedActionIndexes)
            {
                bool didItWork = TryToDoAction((AntAction)actionIndex);
                if (didItWork)
                    return;
            }
        }

        // tries to do a specific action, returns true if it worked
        private bool TryToDoAction(AntAction whichAction)
        {
            switch (whichAction)
            {
                case AntAction.MoveNorth: return TryToMove(0, 1);
                case AntAction.MoveSouth: return TryToMove(0, -1);
                case AntAction.MoveEast: return TryToMove(1, 0);
                case AntAction.MoveWest: return TryToMove(-1, 0);
                case AntAction.Dig: return TryToDig();
                case AntAction.EatMulch: return TryToEatMulch();
                case AntAction.PlaceNest: return TryToPlaceNest();
                case AntAction.ShareHealth: return TryToShareHealth();
                case AntAction.DoNothing: return true; // doing nothing always works
                default: return false;
            }
        }

        // tries to move the ant one step in a direction
        // cant move if the height difference is more than 2 blocks
        private bool TryToMove(int moveInX, int moveInZ)
        {
            int destinationX = positionX + moveInX;
            int destinationZ = positionZ + moveInZ;

            // find what height the ground is at where we want to go
            int destinationY = FindTopBlockY(destinationX, destinationZ);

            // check if the height difference is too big (more than 2 blocks)
            if (Mathf.Abs(destinationY - positionY) > 2)
                return false;

            // make sure there is actually ground there, not just air
            AbstractBlock groundAtDestination = WorldManager.Instance.GetBlock(destinationX, destinationY, destinationZ);
            if (groundAtDestination is AirBlock)
                return false;

            // everything looks good, move the ant
            positionX = destinationX;
            positionY = destinationY;
            positionZ = destinationZ;

            // leave a little bit of pheromone trail
            theColonyManager.AddPheromone(positionX, positionZ, 0.05f);

            return true;
        }

        // tries to dig up the block the ant is standing on
        // cant dig container blocks, and after digging the ant falls down
        private bool TryToDig()
        {
            AbstractBlock blockToRemove = WorldManager.Instance.GetBlock(positionX, positionY, positionZ);

            // cant dig container blocks, those are indestructible
            if (blockToRemove is ContainerBlock)
                return false;

            // cant dig air, theres nothing there
            if (blockToRemove is AirBlock)
                return false;

            // check if we're about to destroy a nest block so we can tell the manager
            bool wasItANestBlock = blockToRemove is NestBlock;

            // remove the block by replacing it with air
            WorldManager.Instance.SetBlock(positionX, positionY, positionZ, new AirBlock());

            // if it was a nest block, tell the manager we lost one
            if (wasItANestBlock)
                theColonyManager.NestBlockWasDestroyed();

            // ant falls down to whatever is below
            positionY = FindTopBlockY(positionX, positionZ);

            return true;
        }

        // tries to eat the mulch block the ant is standing on
        // cant eat if another ant is on the same block
        private bool TryToEatMulch()
        {
            AbstractBlock blockBelow = WorldManager.Instance.GetBlock(positionX, positionY, positionZ);

            // can only eat if standing on mulch
            if (!(blockBelow is MulchBlock))
                return false;

            // cant eat if theres another ant here too
            if (theColonyManager.IsThereAnotherAntHere(this, positionX, positionY, positionZ))
                return false;

            // eat the mulch - get health back and remove the mulch block
            currentHealth = Mathf.Min(currentHealth + ConfigurationManager.Instance.MulchHealthRestore, maximumHealth);
            WorldManager.Instance.SetBlock(positionX, positionY, positionZ, new AirBlock());

            // fall down to whatever is below the mulch
            positionY = FindTopBlockY(positionX, positionZ);

            // leave a big pheromone mark because we found food here
            theColonyManager.AddPheromone(positionX, positionZ, 0.4f);

            return true;
        }

        // tries to place a nest block above the ant (only the queen can do this)
        // it costs the queen 1/3 of her max health
        private bool TryToPlaceNest()
        {
            // only the queen is allowed to make nests
            if (!isThisTheQueen)
                return false;

            // figure out how much health it costs (1/3 of max)
            float healthCost = maximumHealth / 3f;

            // need to have enough health to pay the cost
            if (currentHealth <= healthCost)
                return false;

            // the nest goes in the air block right above the ant
            int nestY = positionY + 1;
            AbstractBlock spaceAbove = WorldManager.Instance.GetBlock(positionX, nestY, positionZ);

            // there needs to be empty air above for the nest
            if (!(spaceAbove is AirBlock))
                return false;

            // make sure we're not going above the world
            int totalWorldHeight = ConfigurationManager.Instance.World_Height * ConfigurationManager.Instance.Chunk_Diameter;
            if (nestY >= totalWorldHeight - 1)
                return false;

            // place the nest block and pay the health cost
            WorldManager.Instance.SetBlock(positionX, nestY, positionZ, new NestBlock());
            currentHealth = currentHealth - healthCost;

            // move the queen up onto the new nest block
            positionY = nestY;

            // tell the manager we placed a nest
            theColonyManager.NestBlockWasPlaced();

            return true;
        }

        // tries to give some health to another ant at the same spot
        // this is zero-sum, meaning the giver loses exactly what the receiver gets
        private bool TryToShareHealth()
        {
            // dont share if we're already low on health
            if (currentHealth <= maximumHealth * 0.3f)
                return false;

            // look for another ant standing in the same spot
            Ant otherAnt = theColonyManager.GetAnotherAntAtSameSpot(this, positionX, positionY, positionZ);
            if (otherAnt == null)
                return false;

            // figure out how much to share (10% of max health)
            float amountToGive = Mathf.Min(maximumHealth * 0.1f, currentHealth - 1f);
            if (amountToGive <= 0)
                return false;

            // do the transfer - we lose health, they gain the same amount
            currentHealth = currentHealth - amountToGive;
            otherAnt.currentHealth = Mathf.Min(otherAnt.currentHealth + amountToGive, otherAnt.maximumHealth);

            return true;
        }

        // finds the y coordinate of the highest solid block at a given x,z position
        // basically finds "the ground level" at that spot
        private int FindTopBlockY(int checkX, int checkZ)
        {
            int totalWorldHeight = ConfigurationManager.Instance.World_Height * ConfigurationManager.Instance.Chunk_Diameter;
            // start from the top and work down until we find something that isnt air
            for (int y = totalWorldHeight - 1; y >= 0; y--)
            {
                AbstractBlock blockAtThisHeight = WorldManager.Instance.GetBlock(checkX, y, checkZ);
                if (!(blockAtThisHeight is AirBlock))
                    return y;
            }
            return 0; // if everything is air just return the bottom
        }

        // moves the unity game object to match where the ant is in the grid
        // adds a little offset so the ant floats above the block
        public void MoveAntObjectToGridPosition()
        {
            transform.position = new Vector3(positionX, positionY + 0.8f, positionZ);
        }

        // kills the ant and hides it from the game
        private void KillThisAnt()
        {
            isThisAntDead = true;
            currentHealth = 0;
            gameObject.SetActive(false);
            theColonyManager.AntHasDied(this);
        }
    }
}

using System.Collections.Generic;
using UnityEngine;
using Antymology.Terrain;
using Antymology.UI;

namespace Antymology.Agents
{
    // this class manages the whole ant colony simulation
    // it spawns ants, runs the simulation each step, keeps track of fitness
    // and does the genetic algorithm to evolve the ants over generations
    public class AntColonyManager : Singleton<AntColonyManager>
    {
        // stores the best genomes we've seen so far along with how well they did
        private List<SavedGenome> bestGenomesSoFar = new List<SavedGenome>();

        // list of all the ants that are alive right now
        private List<Ant> allCurrentAnts = new List<Ant>();

        // the queen ant (there is only one per generation)
        public Ant theQueen;

        // the genome being used for this generation
        private AntGenome currentGenerationGenome;

        // a 2d grid that stores pheromone levels across the map
        // ants leave pheromones when they walk and when they find food
        private float[,] pheromoneGrid;

        // timer to control how fast the simulation runs
        private float timeUntilNextStep = 0f;

        // is the simulation currently going
        private bool isSimulationRunning = false;

        // has the colony been set up yet
        private bool hasBeenSetUp = false;

        // how many genes are in each genome (depends on neural network size)
        private int numberOfGenesNeeded;

        // which generation number we are on
        public int currentGenerationNumber = 0;

        // how many nest blocks the queen placed this generation
        public int nestsPlacedThisGeneration = 0;

        // total nest blocks that exist in the world right now
        public int totalNestBlocksInWorld = 0;

        // the best number of nests any single generation has ever made
        public int bestNestScoreEver = 0;

        // what simulation step we are on within this generation
        public int currentStepNumber = 0;

        // how many ants are still alive
        public int numberOfAliveAnts = 0;

        // how fast the simulation is running (steps per second)
        public float currentSimulationSpeed;

        // is the simulation paused right now
        public bool isCurrentlyPaused = false;

        // this gets called by the world manager after the world has been built
        // it sets everything up and starts the first generation
        public void InitializeColony()
        {
            if (hasBeenSetUp) return;
            hasBeenSetUp = true;

            currentSimulationSpeed = ConfigurationManager.Instance.SimulationSpeed;

            // figure out how many genes we need by making a temporary neural network
            NeuralNetwork tempNetwork = new NeuralNetwork(
                ConfigurationManager.Instance.NeuralNetInputSize,
                ConfigurationManager.Instance.NeuralNetHiddenSize,
                ConfigurationManager.Instance.NeuralNetOutputSize
            );
            numberOfGenesNeeded = tempNetwork.TotalNumberOfWeights;

            // set up the pheromone grid to be the same size as the world
            int worldWidth = ConfigurationManager.Instance.World_Diameter * ConfigurationManager.Instance.Chunk_Diameter;
            int worldDepth = worldWidth; // world is square
            pheromoneGrid = new float[worldWidth, worldDepth];

            // make the UI that shows nest count and stuff
            GameObject uiObject = new GameObject("NestCountUI");
            uiObject.AddComponent<NestCountUI>();

            // start the very first generation
            BeginNewGeneration();
        }

        // starts a new generation of ants
        // cleans up old ants, makes a new genome (either random or evolved), spawns new ants
        private void BeginNewGeneration()
        {
            // get rid of all the old ants from last generation
            foreach (var oldAnt in allCurrentAnts)
            {
                if (oldAnt != null && oldAnt.gameObject != null)
                    Destroy(oldAnt.gameObject);
            }
            allCurrentAnts.Clear();
            theQueen = null;

            // clear out all pheromones so the new ants start fresh
            if (pheromoneGrid != null)
                System.Array.Clear(pheromoneGrid, 0, pheromoneGrid.Length);

            // reset the counters for this generation
            nestsPlacedThisGeneration = 0;
            currentStepNumber = 0;

            // count how many nest blocks are already in the world from previous generations
            totalNestBlocksInWorld = CountAllNestBlocksInTheWorld();

            // go to next generation
            currentGenerationNumber++;

            // create a genome for this generation (random at first, evolved later)
            currentGenerationGenome = CreateGenomeForThisGeneration();

            // spawn all the ants
            SpawnAllTheAnts(currentGenerationGenome);

            isSimulationRunning = true;

            Debug.Log("Started generation " + currentGenerationNumber + " with " + allCurrentAnts.Count + " ants");
        }

        // when a generation ends, save how well it did and start the next one
        private void FinishCurrentGeneration()
        {
            isSimulationRunning = false;

            // save this genome and its fitness score
            bestGenomesSoFar.Add(new SavedGenome(currentGenerationGenome.MakeCopy(), nestsPlacedThisGeneration));

            // sort so the best ones are first
            bestGenomesSoFar.Sort((a, b) => b.fitnessScore.CompareTo(a.fitnessScore));

            // only keep the top few genomes (dont want the list to grow forever)
            int howManyToKeep = ConfigurationManager.Instance.EliteCount;
            while (bestGenomesSoFar.Count > howManyToKeep)
                bestGenomesSoFar.RemoveAt(bestGenomesSoFar.Count - 1);

            // check if this generation beat the record
            if (nestsPlacedThisGeneration > bestNestScoreEver)
                bestNestScoreEver = nestsPlacedThisGeneration;

            Debug.Log("Generation " + currentGenerationNumber + " is done. Nests this gen: " + nestsPlacedThisGeneration +
                      " | Total in world: " + totalNestBlocksInWorld + " | Best ever: " + bestNestScoreEver);

            // wait a tiny bit then start the next generation
            Invoke("BeginNewGeneration", 0.3f);
        }

        // creates a genome for the new generation
        // if we dont have enough saved genomes yet, just makes a random one
        // otherwise picks two good parents and combines them with crossover + mutation
        private AntGenome CreateGenomeForThisGeneration()
        {
            // if we dont have at least 2 saved genomes, just make a random one
            if (bestGenomesSoFar.Count < 2)
                return new AntGenome(numberOfGenesNeeded);

            // pick two parents using tournament selection
            AntGenome parentA = PickAGoodParent();
            AntGenome parentB = PickAGoodParent();

            // mix the two parents together
            AntGenome childGenome = AntGenome.MixTwoGenomes(parentA, parentB);

            // randomly change some genes
            childGenome.RandomlyChangeGenes(
                ConfigurationManager.Instance.MutationRate,
                ConfigurationManager.Instance.MutationStrength
            );

            return childGenome;
        }

        // tournament selection - pick two random saved genomes and return the better one
        private AntGenome PickAGoodParent()
        {
            int randomIndex1 = Random.Range(0, bestGenomesSoFar.Count);
            int randomIndex2 = Random.Range(0, bestGenomesSoFar.Count);

            // return whichever one had a better fitness score
            if (bestGenomesSoFar[randomIndex1].fitnessScore >= bestGenomesSoFar[randomIndex2].fitnessScore)
                return bestGenomesSoFar[randomIndex1].theGenome;
            else
                return bestGenomesSoFar[randomIndex2].theGenome;
        }

        // spawns all ants for this generation near the center of the world
        private void SpawnAllTheAnts(AntGenome baseGenome)
        {
            int howManyAnts = ConfigurationManager.Instance.AntCount;
            int worldWidth = ConfigurationManager.Instance.World_Diameter * ConfigurationManager.Instance.Chunk_Diameter;

            // find the center of the map
            int centerX = worldWidth / 2;
            int centerZ = worldWidth / 2;
            int groundY = FindGroundLevel(centerX, centerZ);

            // spawn the queen in the center
            SpawnOneAnt(baseGenome.MakeCopy(), true, centerX, groundY, centerZ);

            // spawn workers in a ring around the queen
            for (int i = 0; i < howManyAnts - 1; i++)
            {
                // make a slightly different copy of the genome for each worker
                AntGenome workerGenome = baseGenome.MakeCopy();
                workerGenome.RandomlyChangeGenes(0.05f, 0.1f);

                // pick a random spot near the center
                int spawnX = centerX + Random.Range(-8, 9);
                int spawnZ = centerZ + Random.Range(-8, 9);

                // make sure we dont go off the edge of the map
                spawnX = Mathf.Clamp(spawnX, 2, worldWidth - 3);
                spawnZ = Mathf.Clamp(spawnZ, 2, worldWidth - 3);
                int spawnY = FindGroundLevel(spawnX, spawnZ);

                // make sure theres actually ground to stand on
                AbstractBlock groundBlock = WorldManager.Instance.GetBlock(spawnX, spawnY, spawnZ);
                if (groundBlock is AirBlock) continue;

                SpawnOneAnt(workerGenome, false, spawnX, spawnY, spawnZ);
            }

            numberOfAliveAnts = allCurrentAnts.Count;
        }

        // creates one ant in the world
        // queen gets a gold sphere, workers get brown cubes
        private void SpawnOneAnt(AntGenome genomeToUse, bool makeItTheQueen, int startX, int startY, int startZ)
        {
            // make the 3d object - sphere for queen, cube for workers
            GameObject antObject;
            if (makeItTheQueen)
                antObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            else
                antObject = GameObject.CreatePrimitive(PrimitiveType.Cube);

            antObject.name = makeItTheQueen ? "QueenAnt" : "WorkerAnt";

            // remove the collider so it doesnt mess with raycasting
            Collider theCollider = antObject.GetComponent<Collider>();
            if (theCollider != null) Destroy(theCollider);

            // make the queen bigger than the workers
            if (makeItTheQueen)
                antObject.transform.localScale = new Vector3(1.0f, 0.7f, 1.0f);
            else
                antObject.transform.localScale = new Vector3(0.5f, 0.3f, 0.5f);

            // set the color
            MeshRenderer meshRenderer = antObject.GetComponent<MeshRenderer>();
            if (meshRenderer != null)
            {
                Material antMaterial = new Material(Shader.Find("Standard"));
                if (makeItTheQueen)
                {
                    // queen is gold colored and shiny
                    antMaterial.color = new Color(1f, 0.84f, 0f);
                    antMaterial.SetFloat("_Metallic", 0.7f);
                    antMaterial.SetFloat("_Glossiness", 0.7f);
                }
                else
                {
                    // workers are brown
                    antMaterial.color = new Color(0.55f, 0.27f, 0.07f);
                }
                meshRenderer.material = antMaterial;
            }

            // add the ant script and set it up
            Ant newAnt = antObject.AddComponent<Ant>();
            newAnt.Setup(genomeToUse, makeItTheQueen, startX, startY, startZ, this);
            allCurrentAnts.Add(newAnt);

            if (makeItTheQueen)
                theQueen = newAnt;
        }

        // this runs every frame and is the main simulation loop
        private void Update()
        {
            if (!hasBeenSetUp) return;

            // check for speed/pause keyboard input
            CheckForKeyboardInput();

            if (!isSimulationRunning || isCurrentlyPaused) return;

            // figure out when to run the next simulation step based on speed
            timeUntilNextStep = timeUntilNextStep + Time.deltaTime;
            float timeBetweenSteps = 1f / currentSimulationSpeed;

            // dont do too many steps in one frame or it will lag
            int maxStepsThisFrame = Mathf.Max(1, (int)(currentSimulationSpeed / 10f));
            int stepsdoneSoFar = 0;

            while (timeUntilNextStep >= timeBetweenSteps && stepsdoneSoFar < maxStepsThisFrame)
            {
                timeUntilNextStep = timeUntilNextStep - timeBetweenSteps;
                stepsdoneSoFar++;

                DoOneSimulationStep();

                if (!isSimulationRunning) break;
            }

            // dont let the timer build up too much
            if (timeUntilNextStep > timeBetweenSteps * 3)
                timeUntilNextStep = 0f;
        }

        // checks if the player pressed any speed or pause keys
        private void CheckForKeyboardInput()
        {
            // press ] or = to speed up
            if (Input.GetKeyDown(KeyCode.RightBracket) || Input.GetKeyDown(KeyCode.Equals))
            {
                currentSimulationSpeed = Mathf.Min(currentSimulationSpeed * 1.5f, 500f);
            }

            // press [ or - to slow down
            if (Input.GetKeyDown(KeyCode.LeftBracket) || Input.GetKeyDown(KeyCode.Minus))
            {
                currentSimulationSpeed = Mathf.Max(currentSimulationSpeed / 1.5f, 1f);
            }

            // press P to pause or unpause
            if (Input.GetKeyDown(KeyCode.P))
            {
                isCurrentlyPaused = !isCurrentlyPaused;
            }
        }

        // runs one step of the simulation for all ants
        private void DoOneSimulationStep()
        {
            currentStepNumber++;

            // tell each living ant to do their thing
            foreach (var ant in allCurrentAnts)
            {
                if (ant != null && !ant.isThisAntDead)
                    ant.DoOneStep();
            }

            // automatically share health with the queen if workers are nearby
            AutoShareHealthWithQueen();

            // make pheromones fade over time
            FadePheromones();

            // count how many ants are still alive
            numberOfAliveAnts = 0;
            bool anyoneStillAlive = false;
            foreach (var ant in allCurrentAnts)
            {
                if (ant != null && !ant.isThisAntDead)
                {
                    numberOfAliveAnts++;
                    anyoneStillAlive = true;
                }
            }

            // end the generation if everyone is dead or we hit the step limit
            if (!anyoneStillAlive || currentStepNumber >= ConfigurationManager.Instance.StepsPerGeneration)
            {
                FinishCurrentGeneration();
            }
        }

        // if workers are standing on the same spot as the queen and shes hurt,
        // they automatically give her some health
        private void AutoShareHealthWithQueen()
        {
            if (theQueen == null || theQueen.isThisAntDead) return;

            foreach (var worker in allCurrentAnts)
            {
                if (worker == null || worker.isThisAntDead || worker.isThisTheQueen) continue;

                // worker needs to be at the same spot as the queen
                if (worker.positionX == theQueen.positionX && worker.positionY == theQueen.positionY && worker.positionZ == theQueen.positionZ)
                {
                    // only share if queen health is below 60% and worker has enough health
                    if (theQueen.currentHealth < theQueen.maximumHealth * 0.6f && worker.currentHealth > worker.maximumHealth * 0.4f)
                    {
                        float amountToGive = Mathf.Min(worker.maximumHealth * 0.1f, worker.currentHealth - 1f);
                        if (amountToGive > 0)
                        {
                            worker.currentHealth = worker.currentHealth - amountToGive;
                            theQueen.currentHealth = Mathf.Min(theQueen.currentHealth + amountToGive, theQueen.maximumHealth);
                        }
                    }
                }
            }
        }

        // returns how strong the pheromone is at a spot on the map
        public float GetPheromoneStrength(int xPos, int zPos)
        {
            if (pheromoneGrid == null) return 0f;
            if (xPos < 0 || zPos < 0 || xPos >= pheromoneGrid.GetLength(0) || zPos >= pheromoneGrid.GetLength(1))
                return 0f;
            return pheromoneGrid[xPos, zPos];
        }

        // adds pheromone at a spot on the map (ants call this when walking or finding food)
        public void AddPheromone(int xPos, int zPos, float howMuch)
        {
            if (pheromoneGrid == null) return;
            if (xPos < 0 || zPos < 0 || xPos >= pheromoneGrid.GetLength(0) || zPos >= pheromoneGrid.GetLength(1))
                return;
            pheromoneGrid[xPos, zPos] = Mathf.Min(pheromoneGrid[xPos, zPos] + howMuch, 1f);
        }

        // makes all pheromones fade a little bit each step
        private void FadePheromones()
        {
            if (pheromoneGrid == null) return;

            float fadeRate = ConfigurationManager.Instance.PheromoneDecayRate;
            int gridWidth = pheromoneGrid.GetLength(0);
            int gridDepth = pheromoneGrid.GetLength(1);

            for (int x = 0; x < gridWidth; x++)
            {
                for (int z = 0; z < gridDepth; z++)
                {
                    if (pheromoneGrid[x, z] > 0.001f)
                    {
                        // reduce pheromone by the fade rate
                        pheromoneGrid[x, z] = pheromoneGrid[x, z] * (1f - fadeRate);
                    }
                    else
                    {
                        // if its really small just set it to zero
                        pheromoneGrid[x, z] = 0f;
                    }
                }
            }
        }

        // finds the y position of the ground at a given x,z spot
        public int FindGroundLevel(int xPos, int zPos)
        {
            int worldHeight = ConfigurationManager.Instance.World_Height * ConfigurationManager.Instance.Chunk_Diameter;
            for (int y = worldHeight - 1; y >= 0; y--)
            {
                AbstractBlock blockHere = WorldManager.Instance.GetBlock(xPos, y, zPos);
                if (!(blockHere is AirBlock))
                    return y;
            }
            return 0;
        }

        // checks if any other ant (besides the one asking) is at the same spot
        public bool IsThereAnotherAntHere(Ant askingAnt, int checkX, int checkY, int checkZ)
        {
            foreach (var otherAnt in allCurrentAnts)
            {
                if (otherAnt == null || otherAnt == askingAnt || otherAnt.isThisAntDead) continue;
                if (otherAnt.positionX == checkX && otherAnt.positionY == checkY && otherAnt.positionZ == checkZ)
                    return true;
            }
            return false;
        }

        // finds another ant at the same spot (picks the one with lowest health)
        // used for health sharing so we help the ant that needs it most
        public Ant GetAnotherAntAtSameSpot(Ant askingAnt, int checkX, int checkY, int checkZ)
        {
            Ant antWithLowestHealth = null;
            float lowestHealthFound = float.MaxValue;

            foreach (var otherAnt in allCurrentAnts)
            {
                if (otherAnt == null || otherAnt == askingAnt || otherAnt.isThisAntDead) continue;
                if (otherAnt.positionX == checkX && otherAnt.positionY == checkY && otherAnt.positionZ == checkZ)
                {
                    if (otherAnt.currentHealth < lowestHealthFound)
                    {
                        lowestHealthFound = otherAnt.currentHealth;
                        antWithLowestHealth = otherAnt;
                    }
                }
            }
            return antWithLowestHealth;
        }

        // goes through the entire world and counts how many nest blocks there are
        public int CountAllNestBlocksInTheWorld()
        {
            int nestCount = 0;
            int worldWidth = ConfigurationManager.Instance.World_Diameter * ConfigurationManager.Instance.Chunk_Diameter;
            int worldHeight = ConfigurationManager.Instance.World_Height * ConfigurationManager.Instance.Chunk_Diameter;

            for (int x = 0; x < worldWidth; x++)
                for (int y = 0; y < worldHeight; y++)
                    for (int z = 0; z < worldWidth; z++)
                        if (WorldManager.Instance.GetBlock(x, y, z) is NestBlock)
                            nestCount++;

            return nestCount;
        }

        // called when the queen places a nest block
        public void NestBlockWasPlaced()
        {
            nestsPlacedThisGeneration++;
            totalNestBlocksInWorld++;
        }

        // called when an ant digs up a nest block
        public void NestBlockWasDestroyed()
        {
            totalNestBlocksInWorld = Mathf.Max(0, totalNestBlocksInWorld - 1);
        }

        // called when an ant dies
        public void AntHasDied(Ant deadAnt)
        {
            if (deadAnt.isThisTheQueen)
                Debug.Log("The queen died in generation " + currentGenerationNumber + " at step " + currentStepNumber);
        }

        // this little class just holds a genome and its fitness score together
        // used for keeping track of the best genomes
        private class SavedGenome
        {
            public AntGenome theGenome;
            public int fitnessScore;

            public SavedGenome(AntGenome genome, int fitness)
            {
                theGenome = genome;
                fitnessScore = fitness;
            }
        }
    }
}

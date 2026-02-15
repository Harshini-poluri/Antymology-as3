# Antymology - Ant Colony Simulation

> CPSC 565 Assignment 3 - Winter 2026

![Ants](Images/Ants.gif)

---

## What Is This?

This is a simulation where a colony of ants try to build the biggest nest they can. There is one queen ant and a bunch of worker ants. The queen is the only one who can make nest blocks, and the workers need to help her survive by finding food and sharing health with her.

The cool part is that the ants are controlled by a neural network, and I used a genetic algorithm to evolve them over time. So at first the ants just do random stuff, but after a bunch of generations they start to get better at finding food and building nests.

I built this in Unity using C#. The base world generation code was provided by the instructor (forked from [DaviesCooper/Antymology](https://github.com/DaviesCooper/Antymology)) and I added all the ant behaviour, neural network, and evolution code on top of it.

---

## How It Works

### The Ants

Each ant has health that goes down a little bit every step. If their health hits 0 they die and get removed. Ants can eat mulch blocks to get health back, but they have to be standing right on top of the mulch and no other ant can be on the same block.

Ants can move in 4 directions (north, south, east, west) but they cant climb more than 2 blocks high. They can also dig blocks to remove them from the world (except container blocks which are indestructible). Standing on acid blocks makes health drain twice as fast.

The queen ant looks different from the workers - she is a big gold sphere while workers are small brown cubes. The queen can place nest blocks which is how the colony scores points, but each nest costs 1/3 of her max health. Workers can share health with the queen to keep her alive.

### Neural Network

Each ant has a small neural network brain that takes in 18 inputs (things like health level, what blocks are nearby, where the queen is, etc) and outputs 9 possible actions. The action with the highest score gets tried first, and if that action is invalid it tries the next one.

The neural network has one hidden layer with 12 neurons and uses tanh activation. The total number of weights and biases is 345 which is the genome size.

### Evolution / Genetic Algorithm

The simulation runs in generations. Each generation spawns a colony of 25 ants that all share the same base genome (with tiny random variations per ant). They run for up to 1500 steps, and the fitness score is how many nest blocks got placed.

After each generation, the genome and its score get saved. The best 6 genomes are kept around. To make a new genome for the next generation, I pick two parents using tournament selection (pick 2 random saved genomes, take the better one) and do two-point crossover to mix them. Then each gene has a 15% chance of getting a small random mutation.

Over many generations the ants should get better at surviving and building nests.

### Pheromones

Ants leave pheromone trails as they walk around. When an ant eats mulch (food) it leaves a stronger pheromone mark. The pheromone levels fade over time. The neural network can see pheromone levels in each direction, so evolved ants might learn to follow pheromone trails to find food faster.

---

## Project Structure

```
Assets/
├── Components/
│   ├── Agents/                     <- my code goes here
│   │   ├── Ant.cs                  # individual ant logic
│   │   ├── AntColonyManager.cs     # manages the colony and evolution
│   │   ├── AntGenome.cs            # genome with crossover and mutation
│   │   └── NeuralNetwork.cs        # simple neural network
│   ├── Configuration/
│   │   └── ConfigurationManager.cs # all the settings
│   ├── Terrain/
│   │   ├── Blocks/                 # different block types
│   │   ├── Chunk.cs                # mesh stuff for rendering
│   │   └── WorldManager.cs         # world generation
│   └── UI/
│       ├── FlyCamera.cs            # camera you can fly around with
│       ├── NestCountUI.cs          # shows stats on screen
│       └── UITerrainEditor.cs      # block placement tool
├── Helpers/
│   ├── CustomMath.cs
│   ├── NoiseGenerator.cs
│   └── Singleton.cs
├── Resources/
│   ├── blockMaterial.mat
│   └── tilesheet.png
└── Scenes/
    └── SampleScene.unity
```

---

## How To Run It

1. You need Unity 6000.3.x installed
2. Clone this repo
3. Open the project in Unity
4. Open the scene at `Assets/Scenes/SampleScene.unity`
5. Hit the Play button

 

## What I Changed From The Base Code

The base repo from the instructor had the world generation, camera, and terrain editor already done. I added:

- All 4 files in the Agents folder (Ant.cs, AntColonyManager.cs, AntGenome.cs, NeuralNetwork.cs)
- NestCountUI.cs for showing stats on screen
- NestBlock.cs for the nest block type
- Added ant/evolution/neural network settings to ConfigurationManager.cs
- Changed WorldManager.cs to call my colony manager instead of throwing NotImplementedException
- Wrapped a MeshUtility call in Chunk.cs with #if UNITY_EDITOR so it builds properly



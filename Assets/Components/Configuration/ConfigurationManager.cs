using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Central configuration for all simulation parameters.
/// Includes world generation, ant behaviour, evolution, and neural network settings.
/// All values are editable in the Unity Inspector at runtime.
/// </summary>
public class ConfigurationManager : Singleton<ConfigurationManager>
{

    #region World Generation

    /// <summary>
    /// The seed for world generation.
    /// </summary>
    public int Seed = 1337;

    /// <summary>
    /// The number of chunks in the x and z dimension of the world.
    /// </summary>
    public int World_Diameter = 16;

    /// <summary>
    /// The number of chunks in the y dimension of the world.
    /// </summary>
    public int World_Height = 4;

    /// <summary>
    /// The number of blocks in any dimension of a chunk.
    /// </summary>
    public int Chunk_Diameter = 8;

    /// <summary>
    /// How much of the tile map does each tile take up.
    /// </summary>
    public float Tile_Map_Unit_Ratio = 0.25f;

    /// <summary>
    /// The number of acidic regions on the map.
    /// </summary>
    public int Number_Of_Acidic_Regions = 10;

    /// <summary>
    /// The radius of each acidic region
    /// </summary>
    public int Acidic_Region_Radius = 5;

    /// <summary>
    /// The number of container spheres on the map.
    /// </summary>
    public int Number_Of_Conatiner_Spheres = 5;

    /// <summary>
    /// The radius of each container sphere.
    /// </summary>
    public int Conatiner_Sphere_Radius = 20;

    #endregion

    // --- everything below here is stuff i added for the ant simulation ---

    // how many ants to spawn each generation (including the queen)
    [Header("Ant Settings")]
    public int AntCount = 25;

    // the most health an ant can have
    public float AntMaxHealth = 100f;

    // how much health each ant loses every step (doubled on acid)
    public float HealthDrainPerStep = 0.5f;

    // how much health eating mulch gives back
    public float MulchHealthRestore = 40f;

    // how many steps happen per second (you can change this with [ and ] keys too)
    [Header("Simulation Settings")]
    public float SimulationSpeed = 30f;

    // how many steps before the generation ends automatically
    public int StepsPerGeneration = 1500;

    // how many of the best genomes to save for making new ones
    [Header("Evolution Settings")]
    public int EliteCount = 6;

    // chance that each gene gets randomly changed (0 to 1)
    public float MutationRate = 0.15f;

    // how big the random changes to genes can be
    public float MutationStrength = 0.5f;

    // number of inputs going into the neural network (18 things the ant can see)
    [Header("Neural Network Settings")]
    public int NeuralNetInputSize = 18;

    // number of neurons in the hidden layer
    public int NeuralNetHiddenSize = 12;

    // number of outputs from the neural network (9 possible actions)
    public int NeuralNetOutputSize = 9;

    // how fast pheromones fade away each step
    [Header("Pheromone Settings")]
    public float PheromoneDecayRate = 0.02f;
}

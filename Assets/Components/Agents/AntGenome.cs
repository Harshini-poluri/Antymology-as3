using System;
using UnityEngine;

namespace Antymology.Agents
{
    // this stores the genes for one ant colony
    // the genes are just a bunch of float numbers that become the neural network weights
    // we can mix two genomes together (crossover) and randomly change them (mutation)
    [System.Serializable]
    public class AntGenome
    {
        // the actual gene values, each one is a float number
        public float[] genes;

        // random number generator that all genomes share
        private static System.Random randomGenerator = new System.Random();

        // makes a new genome with random values between -1 and 1
        public AntGenome(int howManyGenes)
        {
            genes = new float[howManyGenes];
            for (int i = 0; i < howManyGenes; i++)
            {
                // random number between -1 and 1
                genes[i] = (float)(randomGenerator.NextDouble() * 2.0 - 1.0);
            }
        }

        // makes a genome from an existing array of genes (copies them)
        public AntGenome(float[] existingGeneValues)
        {
            genes = (float[])existingGeneValues.Clone();
        }

        // takes two parent genomes and mixes them together to make a child
        // uses two-point crossover - picks two random spots and swaps the middle section
        public static AntGenome MixTwoGenomes(AntGenome parent1, AntGenome parent2)
        {
            float[] childGenes = new float[parent1.genes.Length];

            // pick two random points to split at
            int splitPoint1 = randomGenerator.Next(0, parent1.genes.Length);
            int splitPoint2 = randomGenerator.Next(0, parent1.genes.Length);

            // make sure point1 comes before point2
            if (splitPoint1 > splitPoint2)
            {
                int temp = splitPoint1;
                splitPoint1 = splitPoint2;
                splitPoint2 = temp;
            }

            // copy genes from parents - middle section from parent2, rest from parent1
            for (int i = 0; i < childGenes.Length; i++)
            {
                if (i >= splitPoint1 && i <= splitPoint2)
                    childGenes[i] = parent2.genes[i];
                else
                    childGenes[i] = parent1.genes[i];
            }

            return new AntGenome(childGenes);
        }

        // randomly changes some genes by a small amount
        // chanceOfMutation is how likely each gene is to change (0 to 1)
        // howMuchToChange is the max amount it can change by
        public void RandomlyChangeGenes(float chanceOfMutation, float howMuchToChange)
        {
            for (int i = 0; i < genes.Length; i++)
            {
                // roll the dice - should we mutate this gene?
                if (randomGenerator.NextDouble() < chanceOfMutation)
                {
                    // add a small random change to the gene
                    float changeAmount = (float)((randomGenerator.NextDouble() + randomGenerator.NextDouble() + randomGenerator.NextDouble()) / 3.0 * 2.0 - 1.0)
                                     * howMuchToChange;
                    genes[i] = genes[i] + changeAmount;

                    // keep genes within a reasonable range
                    genes[i] = Mathf.Clamp(genes[i], -3f, 3f);
                }
            }
        }

        // makes a copy of this genome
        public AntGenome MakeCopy()
        {
            return new AntGenome(genes);
        }
    }
}

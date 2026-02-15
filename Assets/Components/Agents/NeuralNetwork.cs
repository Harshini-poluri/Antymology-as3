using System;

namespace Antymology.Agents
{
    // this is a simple neural network with one hidden layer
    // the ant uses this to decide what to do based on what it sees
    [System.Serializable]
    public class NeuralNetwork
    {
        // how many inputs the network takes in
        private int numberOfInputs;

        // how many hidden neurons in the middle layer
        private int numberOfHiddenNeurons;

        // how many outputs the network gives back
        private int numberOfOutputs;

        // weights connecting inputs to hidden layer
        private float[,] inputToHiddenWeights;

        // biases for each hidden neuron
        private float[] hiddenBiases;

        // weights connecting hidden layer to outputs
        private float[,] hiddenToOutputWeights;

        // biases for each output neuron
        private float[] outputBiases;

        // total number of weights and biases in the whole network
        // this is how big the genome needs to be
        public int TotalNumberOfWeights { get; private set; }

        // creates a new neural network with the given sizes
        public NeuralNetwork(int inputCount, int hiddenCount, int outputCount)
        {
            numberOfInputs = inputCount;
            numberOfHiddenNeurons = hiddenCount;
            numberOfOutputs = outputCount;

            // make arrays to hold all the weights and biases
            inputToHiddenWeights = new float[inputCount, hiddenCount];
            hiddenBiases = new float[hiddenCount];
            hiddenToOutputWeights = new float[hiddenCount, outputCount];
            outputBiases = new float[outputCount];

            // calculate total number of parameters
            TotalNumberOfWeights = (inputCount * hiddenCount) + hiddenCount +
                         (hiddenCount * outputCount) + outputCount;
        }

        // takes the genome and puts all the gene values into the weights and biases
        public void LoadWeightsFromGenome(AntGenome theGenome)
        {
            int geneIndex = 0;

            // first load input to hidden weights
            for (int i = 0; i < numberOfInputs; i++)
                for (int j = 0; j < numberOfHiddenNeurons; j++)
                    inputToHiddenWeights[i, j] = theGenome.genes[geneIndex++];

            // then load hidden biases
            for (int j = 0; j < numberOfHiddenNeurons; j++)
                hiddenBiases[j] = theGenome.genes[geneIndex++];

            // then load hidden to output weights
            for (int j = 0; j < numberOfHiddenNeurons; j++)
                for (int k = 0; k < numberOfOutputs; k++)
                    hiddenToOutputWeights[j, k] = theGenome.genes[geneIndex++];

            // finally load output biases
            for (int k = 0; k < numberOfOutputs; k++)
                outputBiases[k] = theGenome.genes[geneIndex++];
        }

        // runs the inputs through the network and returns the outputs
        // uses tanh as the activation function
        public float[] RunForward(float[] inputValues)
        {
            // first calculate the hidden layer values
            float[] hiddenValues = new float[numberOfHiddenNeurons];
            for (int j = 0; j < numberOfHiddenNeurons; j++)
            {
                float total = hiddenBiases[j];
                for (int i = 0; i < numberOfInputs; i++)
                    total = total + inputValues[i] * inputToHiddenWeights[i, j];
                hiddenValues[j] = (float)Math.Tanh(total);
            }

            // then calculate the output layer values
            float[] outputValues = new float[numberOfOutputs];
            for (int k = 0; k < numberOfOutputs; k++)
            {
                float total = outputBiases[k];
                for (int j = 0; j < numberOfHiddenNeurons; j++)
                    total = total + hiddenValues[j] * hiddenToOutputWeights[j, k];
                outputValues[k] = (float)Math.Tanh(total);
            }

            return outputValues;
        }
    }
}

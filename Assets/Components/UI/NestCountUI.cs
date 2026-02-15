using UnityEngine;
using Antymology.Agents;

namespace Antymology.UI
{
    // this draws all the simulation info on screen so you can see whats happening
    // it shows nest count, generation number, how many ants are alive, etc
    public class NestCountUI : MonoBehaviour
    {
        // style for the dark background box
        private GUIStyle backgroundBoxStyle;

        // style for the title text
        private GUIStyle titleTextStyle;

        // style for the regular info text
        private GUIStyle infoTextStyle;

        // style for the small controls text at the bottom
        private GUIStyle smallTextStyle;

        // have we set up the styles yet
        private bool stylesAreReady = false;

        // sets up all the text styles and colors
        private void SetUpStyles()
        {
            // dark semi-transparent background
            backgroundBoxStyle = new GUIStyle(GUI.skin.box);
            Texture2D darkBackground = new Texture2D(1, 1);
            darkBackground.SetPixel(0, 0, new Color(0f, 0f, 0f, 0.75f));
            darkBackground.Apply();
            backgroundBoxStyle.normal.background = darkBackground;
            backgroundBoxStyle.padding = new RectOffset(12, 12, 8, 8);

            // gold colored title
            titleTextStyle = new GUIStyle(GUI.skin.label);
            titleTextStyle.fontSize = 18;
            titleTextStyle.fontStyle = FontStyle.Bold;
            titleTextStyle.normal.textColor = new Color(1f, 0.84f, 0f);
            titleTextStyle.alignment = TextAnchor.MiddleLeft;

            // white info text
            infoTextStyle = new GUIStyle(GUI.skin.label);
            infoTextStyle.fontSize = 14;
            infoTextStyle.normal.textColor = Color.white;
            infoTextStyle.richText = true;

            // grey small text for controls
            smallTextStyle = new GUIStyle(GUI.skin.label);
            smallTextStyle.fontSize = 11;
            smallTextStyle.normal.textColor = new Color(0.7f, 0.7f, 0.7f);
            smallTextStyle.richText = true;

            stylesAreReady = true;
        }

        // this gets called every frame to draw the UI
        private void OnGUI()
        {
            if (!stylesAreReady)
                SetUpStyles();

            AntColonyManager colonyManager = AntColonyManager.Instance;
            if (colonyManager == null) return;

            // draw the main info panel in the top left corner
            float panelWidth = 280f;
            float panelHeight = 240f;
            Rect panelArea = new Rect(10, 10, panelWidth, panelHeight);

            GUI.Box(panelArea, "", backgroundBoxStyle);
            GUILayout.BeginArea(new Rect(panelArea.x + 12, panelArea.y + 8, panelWidth - 24, panelHeight - 16));
            GUILayout.BeginVertical();

            // title
            GUILayout.Label("Antymology", titleTextStyle);
            GUILayout.Space(4);

            // show all the stats
            int maxSteps = ConfigurationManager.Instance.StepsPerGeneration;
            string pausedText = colonyManager.isCurrentlyPaused ? " <color=red>[PAUSED]</color>" : "";

            GUILayout.Label("<b>Generation:</b> " + colonyManager.currentGenerationNumber + pausedText, infoTextStyle);
            GUILayout.Label("<b>Step:</b> " + colonyManager.currentStepNumber + " / " + maxSteps, infoTextStyle);
            GUILayout.Space(4);
            GUILayout.Label("<b>Nest Blocks (World):</b> " + colonyManager.totalNestBlocksInWorld, infoTextStyle);
            GUILayout.Label("<b>Nest Blocks (This Gen):</b> " + colonyManager.nestsPlacedThisGeneration, infoTextStyle);
            GUILayout.Label("<b>Best Generation Fitness:</b> " + colonyManager.bestNestScoreEver, infoTextStyle);
            GUILayout.Space(4);
            GUILayout.Label("<b>Alive Ants:</b> " + colonyManager.numberOfAliveAnts, infoTextStyle);
            GUILayout.Label("<b>Sim Speed:</b> " + colonyManager.currentSimulationSpeed.ToString("F0") + " steps/s", infoTextStyle);

            GUILayout.FlexibleSpace();

            // show the keyboard controls
            GUILayout.Label("] or = : Faster   [ or - : Slower   P : Pause", smallTextStyle);

            GUILayout.EndVertical();
            GUILayout.EndArea();

            // draw the queen's health bar in the top right corner (only if queen is alive)
            if (colonyManager.theQueen != null && !colonyManager.theQueen.isThisAntDead)
            {
                float healthBarWidth = 200f;
                float healthBarHeight = 30f;
                Rect healthBarArea = new Rect(Screen.width - healthBarWidth - 20, 10, healthBarWidth, healthBarHeight + 30);

                GUI.Box(healthBarArea, "", backgroundBoxStyle);
                GUILayout.BeginArea(new Rect(healthBarArea.x + 10, healthBarArea.y + 5, healthBarWidth - 20, healthBarHeight + 20));

                GUILayout.Label("<b>Queen Health</b>", infoTextStyle);

                // draw the actual health bar
                Rect healthBarBackground = GUILayoutUtility.GetRect(healthBarWidth - 20, 12);
                float healthPercent = colonyManager.theQueen.currentHealth / colonyManager.theQueen.maximumHealth;

                // dark red background
                GUI.DrawTexture(healthBarBackground, MakeColorTexture(1, 1, new Color(0.3f, 0f, 0f)));

                // colored foreground that shrinks as health goes down
                Rect healthBarForeground = new Rect(healthBarBackground.x, healthBarBackground.y, healthBarBackground.width * healthPercent, healthBarBackground.height);
                Color barColor = Color.Lerp(Color.red, Color.green, healthPercent);
                GUI.DrawTexture(healthBarForeground, MakeColorTexture(1, 1, barColor));

                GUILayout.EndArea();
            }
        }

        // helper to make a tiny texture filled with one color (for the health bar)
        private Texture2D MakeColorTexture(int width, int height, Color fillColor)
        {
            Texture2D newTexture = new Texture2D(width, height);
            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                    newTexture.SetPixel(x, y, fillColor);
            newTexture.Apply();
            return newTexture;
        }
    }
}

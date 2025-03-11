using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using KModkit;
using Rnd = UnityEngine.Random;

public class halfwayKeypadScript : MonoBehaviour {

    public KMBombModule Module;
    public KMBombInfo Bomb;
    public KMAudio Audio;

    public GameObject[] Objects;
    public KMSelectable[] Buttons;
    public Sprite[] Symbols;
    public SpriteRenderer[] Slots;
    public Material[] Mats;
    public MeshRenderer[] LEDs;

    int[][] TableSymbols = new int[][] {
        new int[] { 3,  21, 19, 7,  3,  9,  12, 11, 22, 13, 30, 18 },
        new int[] { 8,  32, 9,  1,  25, 29, 33, 5,  23, 29, 11, 33 },
        new int[] { 25, 0,  23, 0,  19, 32, 34, 15, 4,  12, 15, 1  },
        new int[] { 21, 22, 19, 29, 2,  16, 33, 10, 35, 20, 1,  17 },
        new int[] { 4,  29, 11, 13, 32, 21, 4,  27, 18, 6,  30, 5  },
        new int[] { 23, 28, 33, 34, 3,  20, 30, 1,  8,  32, 31, 2  },
        new int[] { 14, 34, 3,  19, 5,  7,  6,  25, 12, 18, 7,  13 },
        new int[] { 27, 26, 12, 24, 0,  9,  15, 28, 17, 35, 15, 16 },
        new int[] { 10, 24, 8,  31, 23, 14, 26, 11, 22, 9,  0,  25 },
        new int[] { 17, 4,  22, 20, 21, 28, 31, 27, 26, 35, 6,  31 },
        new int[] { 27, 34, 16, 2,  14, 8,  10, 30, 6,  28, 7,  5  },
        new int[] { 24, 10, 26, 18, 35, 24, 16, 13, 17, 2,  20, 14 }
    };
    int[][] TableArrows = new int[][] {
        new int[] { 3, 3, 3, 3, 2, 2, 3, 3, 2, 2, 2, 2 },
        new int[] { 3, 3, 3, 3, 2, 3, 3, 3, 2, 2, 2, 2 },
        new int[] { 3, 3, 3, 2, 2, 2, 2, 3, 2, 2, 2, 2 },
        new int[] { 1, 3, 1, 0, 3, 3, 0, 2, 3, 2, 0, 2 },
        new int[] { 3, 1, 1, 3, 1, 2, 0, 2, 3, 2, 0, 2 },
        new int[] { 1, 3, 1, 0, 0, 3, 3, 1, 2, 0, 2, 2 },
        new int[] { 3, 3, 1, 0, 1, 1, 3, 1, 0, 0, 2, 0 },
        new int[] { 3, 3, 1, 2, 1, 1, 1, 2, 3, 2, 0, 2 },
        new int[] { 3, 3, 1, 3, 0, 2, 2, 0, 0, 0, 0, 0 },
        new int[] { 1, 1, 1, 1, 0, 1, 1, 0, 0, 0, 0, 0 },
        new int[] { 1, 1, 1, 1, 1, 0, 0, 1, 1, 0, 0, 0 },
        new int[] { 1, 1, 1, 1, 1, 0, 0, 1, 0, 0, 0, 0 }
    };
    bool[] pressed = { false, false, false, false };
    string[] textSymbols = { "1-copyright", "2-filledstar", "3-hollowstar", "4-smileyface", "5-doublek", "6-omega", "7-squidknife", "8-pumpkin", "9-hookn", "10-teepee", "11-six", "12-squigglyn", "13-at", "14-ae", "15-meltedthree", "16-euro", "17-circle", "18-nwithhat", "19-dragon", "20-questionmark", "21-paragraph", "22-rightc", "23-leftc", "24-pitchfork", "25-tripod", "26-cursive", "27-tracks", "28-balloon", "29-weirdnose", "30-upsidedowny", "31-bt", "32-clover", "33-asterisk", "34-weirdbike", "35-speechbubble", "36-crucible" };
    string[] forLogging = { "", "", "", "" };
    int amountPressed = 0;
    int[] correctOrder = { -1, -1, -1 };

    static int moduleIdCounter = 1;
    int moduleId;
    private bool moduleSolved;

    void Awake () {
        moduleId = moduleIdCounter++;

        foreach (KMSelectable Button in Buttons) {
            Button.OnInteract += delegate () { ButtonPress(Button); return false; };
        }
    }

    void Start () {
        int chosenRow = Rnd.Range(0, 12);
        int chosenCol = Rnd.Range(0, 12);
        int chosenSymbol = TableSymbols[chosenRow][chosenCol];
        int chosenArrow = TableArrows[chosenRow][chosenCol];

        pressed[chosenArrow] = true;
        Slots[chosenArrow].sprite = Symbols[chosenSymbol];
        forLogging[chosenArrow] = textSymbols[chosenSymbol];
        LEDs[chosenArrow].material = Mats[2];
        Objects[chosenArrow].transform.localPosition = new Vector3(0.0035f, -0.01f, 0);

        List<int> diagonal = new List<int> { };
        int rayRow = 0 + chosenRow;
        int rayCol = 0 + chosenCol;
        while (true) {
            switch (chosenArrow) {
                case 0: rayRow--; rayCol--; break;
                case 1: rayRow--; rayCol++; break;
                case 2: rayRow++; rayCol--; break;
                case 3: rayRow++; rayCol++; break;
            }
            if (rayRow < 0 || rayRow > 11 || rayCol < 0 || rayCol > 11) { break; }
            int raySymbol = TableSymbols[rayRow][rayCol];
            if (diagonal.Contains(raySymbol) || raySymbol == chosenSymbol) { continue; }
            diagonal.Add(raySymbol);
        }

        int[] indexes = new int[diagonal.Count()];
        for (int x = 0; x <  indexes.Length; x++) { indexes[x] = x; }
        indexes = indexes.Shuffle();

        int counter = 0;
        for (int b = 0; b < 4; b++) {
            if (b == chosenArrow) { continue; }
            Slots[b].sprite = Symbols[diagonal[indexes[counter]]];
            forLogging[b] = textSymbols[diagonal[indexes[counter]]];
            counter++;
        }

        correctOrder = FindCorrectOrder(indexes, chosenArrow);

        Debug.LogFormat("[Halfway Keypad #{0}] Symbols: {1}", moduleId, forLogging.Join(" "));
        Debug.LogFormat("[Halfway Keypad #{0}] Pressed button is {1} ({2}-{3})", moduleId, forLogging[chosenArrow], chosenArrow < 2 ? "top" : "bottom", chosenArrow % 2 == 0 ? "left" : "right");
        Debug.LogFormat("[Halfway Keypad #{0}] Correct order: {1} {2} {3}", moduleId, forLogging[correctOrder[0]], forLogging[correctOrder[1]], forLogging[correctOrder[2]]);
    }

    int[] FindCorrectOrder(int[] input, int avoid) {
        int[] considered = { input[0], input[1], input[2] };
        int[] output = { -1, -1, -1 };
        Array.Sort(considered);
        //Debug.Log(input.Join(","));
        //Debug.Log(considered.Join(","));
        int addend = 0;
        for (int b = 0; b < 4; b++) {
            if (b == avoid) { addend++; continue; }
            output[b - addend] = Array.IndexOf(input, considered[b - addend]);
        }
        for (int o = 0; o < 3; o++) {
            if (output[o] >= avoid) {
                output[o]++;
            }
        }
        //Debug.Log(output.Join(","));
        return output;
    }

    void ButtonPress(KMSelectable Button) {
        for (int b = 0; b < 4; b++) {
            if (Button == Buttons[b]) {
                if (pressed[b]) { return; }
                Button.AddInteractionPunch(1f);
                if (correctOrder[amountPressed] == b) {
                    Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, Button.transform);
                    pressed[b] = true;
                    amountPressed++;
                    StartCoroutine(CorrectPress(b));
                    Debug.LogFormat("[Halfway Keypad #{0}] Pressed {1}, that's correct.", moduleId, forLogging[b]);
                    if (amountPressed == 3) {
                        Module.HandlePass();
                        Debug.LogFormat("[Halfway Keypad #{0}] All buttons pressed, module solved.", moduleId);
                    }
                } else {
                    Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.BigButtonPress, Button.transform);
                    StartCoroutine(IncorrectPress(b));
                    Debug.LogFormat("[Halfway Keypad #{0}] Pressed {1}, that's incorrect, strike!", moduleId, forLogging[b]);
                }
            }
        }
    }

    /* These corountines were largely inspired by tandyCake's Keypad Magnified code */

    IEnumerator CorrectPress(int b) {
        while (Objects[b].transform.localPosition.y > -0.01) {
            Objects[b].transform.localPosition -= new Vector3(0, 0.0015f, 0);
            yield return null;
        }
        LEDs[b].material = Mats[2];
        yield return null;
    }

    IEnumerator IncorrectPress(int b) {
        StartCoroutine(IncorrectBounce(b));
        Module.HandleStrike();
        LEDs[b].material = Mats[1];
        yield return new WaitForSecondsRealtime(1f);
        LEDs[b].material = Mats[pressed[b] ? 2 : 0];
        yield return null;
    }

    IEnumerator IncorrectBounce(int b) {
        while (Objects[b].transform.localPosition.y > -0.005) {
            Objects[b].transform.localPosition -= new Vector3(0, 0.00075f, 0);
            yield return null;
        }
        while (Objects[b].transform.localPosition.y < 0) {
            Objects[b].transform.localPosition += new Vector3(0, 0.00075f, 0);
            yield return null;
        }
    }
}

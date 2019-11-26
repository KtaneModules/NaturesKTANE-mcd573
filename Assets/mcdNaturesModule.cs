using System.Collections;
using System.Linq;
using System.Text.RegularExpressions;
using KModkit;
using UnityEngine;
using Rnd = UnityEngine.Random;

public class mcdNaturesModule : MonoBehaviour
{
    public KMBombModule Module;
    public KMBombInfo BombInfo;
    public KMAudio Audio;

    public KMSelectable RebreedButton;
    public KMSelectable BattleButton;

    public TextMesh Parent1Type;
    public TextMesh Parent1Nature;
    public TextMesh Parent2Type;
    public TextMesh Parent2Nature;
    public TextMesh ChildType;
    public TextMesh DisplayedNature;

    private static int _moduleIdCounter = 1;
    private int _moduleId;
    private bool _isSolved;

    // Indexed the same way as _natureNames
    private int _parent1Type;
    private int _parent1Nature;
    private int _parent2Type;
    private int _parent2Nature;
    private int _childType;
    private int _expectedChildNature;   // the correct solution to the module

    // Indexed the same way as _natureOrder
    private int _displayedNature;
    private int[] _natureOrder;

    private static readonly string[] _creatureTypeNames = new[] { "Traplot", "Runtin", "Alot", "Peren", "Morkie" };
    private static readonly string[] _natureNames = new[] { "Adamant", "Afraid", "Stubborn", "Shy", "Neutral" };

    private static readonly int[][] _table = new[]
    {
        new[] { 0, 1, 2, 3, 4 },    // SN digit 0-1
        new[] { 3, 2, 4, 1, 0 },    // SN digit 2-3
        new[] { 4, 1, 0, 3, 2 },    // SN digit 4-5
        new[] { 2, 4, 1, 0, 3 },    // SN digit 6-7
        new[] { 1, 0, 3, 2, 4 }     // SN digit 8-9
    };

    // Use this for initialization
    void Start()
    {
        _moduleId = _moduleIdCounter++;

        RebreedButton.OnInteract += delegate { Rebreed(); return false; };
        BattleButton.OnInteract += delegate { Battle(); return false; };

        _natureOrder = Enumerable.Range(0, _natureNames.Length).ToArray().Shuffle();

        // Decide on the parent and child types, and the parent natures
        _parent1Type = Rnd.Range(0, _creatureTypeNames.Length);
        Parent1Type.text = _creatureTypeNames[_parent1Type];
        _parent2Type = Rnd.Range(0, _creatureTypeNames.Length);
        Parent2Type.text = _creatureTypeNames[_parent2Type];
        _childType = Rnd.Range(0, _creatureTypeNames.Length);
        ChildType.text = _creatureTypeNames[_childType];

        _parent1Nature = Rnd.Range(0, _natureNames.Length);
        Parent1Nature.text = _natureNames[_parent1Nature];
        _parent2Nature = Rnd.Range(0, _natureNames.Length);
        Parent2Nature.text = _natureNames[_parent2Nature];

        Debug.LogFormat(@"[Natures #{0}] Parents are: 1: {1}/{2}, 2: {3}/{4}", _moduleId,
            _creatureTypeNames[_parent1Type], _natureNames[_parent1Nature],
            _creatureTypeNames[_parent2Type], _natureNames[_parent2Nature]);
        Debug.LogFormat(@"[Natures #{0}] Child creature type is: {1}", _moduleId, _creatureTypeNames[_childType]);

        // Calculate the answer (expected child nature)
        var ld = BombInfo.GetSerialNumberNumbers().Last();
        var setToAdamant = false;
        if (ld < 2)
            ld += 0;
        else if (BombInfo.GetSolvableModuleNames().Count(m => m == "Natures") > 1)
            ld += 0;
        else if (BombInfo.GetSolvableModuleNames().Any(m => m == "Monsplode Trading Cards" || m == "Monsplode, Fight!"))
            ld -= 3;
        else if (BombInfo.GetSolvableModuleNames().Any(m => m.ContainsIgnoreCase("Forget") || m.ContainsIgnoreCase("Souvenir")))
            ld += 2;
        else if (BombInfo.GetBatteryCount() > 2)
            ld += 1;
        else if (_childType == 2)    // Alot
            ld -= 2;
        else if (BombInfo.GetSerialNumberLetters().Any(ch => "AEIOU".Contains(ch)))
            ld -= 1;
        else if (_parent1Type == 0 || _parent2Type == 0)    // Traplot
            ld += 5;
        else if (_parent1Nature == 0 || _parent2Nature == 0)    // Adamant
            setToAdamant = true;
        else if (_parent1Nature == 3 || _parent2Nature == 3)   // Shy
            ld = 0;
        else if (_parent1Nature == 1 || _parent2Nature == 1)    // Afraid
            ld -= BombInfo.GetPortPlateCount();
        else if (BombInfo.IsPortPresent(Port.PS2))
            ld += 0;
        else
            ld -= 4;

        if (ld < 0)
            ld = 0;
        else
            ld %= 10;

        if (setToAdamant)
            _expectedChildNature = 0;   // Adamant
        else
            _expectedChildNature = _table[ld / 2][_childType];

        Debug.LogFormat(@"[Natures #{0}] Expected nature is: {1}", _moduleId, _natureNames[_expectedChildNature]);

        _displayedNature = 0;
        UpdateDisplayedNature();
    }

    private void UpdateDisplayedNature()
    {
        DisplayedNature.text = _natureNames[_natureOrder[_displayedNature]];
    }

    private void Battle()
    {
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, BattleButton.transform);
        BattleButton.AddInteractionPunch();

        if (_expectedChildNature == _natureOrder[_displayedNature])
        {
            Debug.LogFormat(@"[Natures #{0}] Selected “Battle!” when the nature was correct. Module solved.", _moduleId);
            Module.HandlePass();
            _isSolved = true;
        }
        else
        {
            Debug.LogFormat(@"[Natures #{0}] Selected “Battle!” when the nature was {1}. Strike!", _moduleId, _natureNames[_natureOrder[_displayedNature]]);
            Module.HandleStrike();
        }
    }

    private void Rebreed()
    {
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, RebreedButton.transform);
        RebreedButton.AddInteractionPunch();

        if (_expectedChildNature == _natureOrder[_displayedNature])
        {
            Debug.LogFormat(@"[Natures #{0}] Selected “Re-Breed” when the nature was already correct. Strike!", _moduleId);
            Module.HandleStrike();
        }
        _displayedNature = (_displayedNature + 1) % _natureOrder.Length;
        UpdateDisplayedNature();
    }

#pragma warning disable 414
    private readonly string TwitchHelpMessage = @"!{0} battle | !{0} rebreed";
#pragma warning restore 414

    KMSelectable[] ProcessTwitchCommand(string command)
    {
        if (Regex.IsMatch(command, @"^\s*battle!?\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
            return new[] { BattleButton };

        if (Regex.IsMatch(command, @"^\s*re-?breed\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
            return new[] { RebreedButton };

        return null;
    }
}

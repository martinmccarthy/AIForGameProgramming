using UnityEngine;
using TMPro;

/* this just uses ascii to swap through the letters idk if theres a more elegant way of doing this but it just made sense in my head -martin */
public class CharacterChanger : MonoBehaviour
{
    [SerializeField] private TMP_Text text;

    private int a = 65;
    private int z = 90;

    private int current;

    private void Start()
    {
        current = a;
    }

    public void CycleLetters(int amount)
    {
        current += amount;
        if (current == a - 1) current = z;
        else if (current == z + 1) current = a;

        char letter = (char)current;
        SetText(letter);
    }

    void SetText(char letter)
    {
        text.text = letter.ToString();
    }
}

using System;
using System.Collections.Generic;
using System.Text;

public class CharacterSelectionEntry
{
    public int pid { get; }
    public string name { get; }
    public bool isValidCharacter = false;
    public bool Initialized = false;

    public CharacterSelectionEntry(int _pid, string _name)
    {
        this.pid = _pid;
        this.name = _name;

        if (pid > 0 && name != "")
            isValidCharacter = true;

        Initialized = true;
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.ComponentModel;
using SummsTracker;

public partial class SROptions
{
    [Category("My Category")]
    public void CreateMatchA()
    {
        DataManager.Instance.CreateMatchTable("testA", "Jose", "Pala");
    }
}
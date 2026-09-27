using UnityEngine;

public static class Idas3Season5Build
{
    public static void VerifyOnly(){
        Debug.Log(Idas3Season5MeterChecks.RunChecks());
        Debug.Log(Idas3MeterDriftChecks.RunChecks());
        Idas3MeterCatalogBuild.VerifyOnly();
        Idas3OrnamentBuild.VerifyOnly();
        Idas3FlexibleOrnamentChecks.VerifyOnly();
    }
    public static void VerifyAndBuild(){VerifyOnly();Idas3Build.RebuildWindowsPlayer();}
}

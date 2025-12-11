using UnityEngine;
using System;

public class TimeBasedLightController : MonoBehaviour
{
    [Header("Référence lumière")]
    public Light directionalLight;          

    [Header("Profils horaires")]
    public TimeLightProfile[] timeProfiles;  

    [Header("Source de l'heure")]
    public bool useRealTime = true;       
    [Tooltip("Format HH:mm:ss, utilisé si useRealTime est false")]
    public string fixedTime = "12:00:00";    

    
    
    void Update()
    {
        if (!Application.isPlaying) return;
        
        string currentTimeStr;
        if (useRealTime)
        {
            currentTimeStr = GameManager.instance.realTime;
        }
        else
        {
            currentTimeStr = fixedTime;
        }
        
        if (!TimeSpan.TryParse(currentTimeStr, out TimeSpan currentTime))
        {
            Debug.LogWarning($"[TimeBasedLightController] Impossible de parser l'heure '{currentTimeStr}'. Format attendu HH:mm:ss.");
            return;
        }

        double currentSec = currentTime.TotalSeconds;
        
        foreach (TimeLightProfile profile in timeProfiles)
        {
            if (!TimeSpan.TryParse(profile.startTime, out TimeSpan start)) continue;
            if (!TimeSpan.TryParse(profile.endTime, out TimeSpan end)) continue;

            double startSec = start.TotalSeconds;
            double endSec = end.TotalSeconds;
            
            if (currentSec >= startSec && currentSec < endSec)
            {
                ApplyProfile(profile, (float)((currentSec - startSec) / (endSec - startSec)));
                return;
            }
            
            if (endSec < startSec)
            {
                if (currentSec >= startSec || currentSec < endSec)
                {
                    double duration = (24 * 3600 - startSec) + endSec;
                    double elapsed;
                    if (currentSec >= startSec)
                        elapsed = currentSec - startSec;
                    else
                        elapsed = (24 * 3600 - startSec) + currentSec;

                    ApplyProfile(profile, (float)(elapsed / duration));
                    return;
                }
            }
        }
    }

    private void ApplyProfile(TimeLightProfile profile, float t)
    {
        t = Mathf.Clamp01(t);
        
        Quaternion rotA = Quaternion.Euler(profile.startRotation);
        Quaternion rotB = Quaternion.Euler(profile.endRotation);
        directionalLight.transform.rotation = Quaternion.Slerp(rotA, rotB, t);
        
        directionalLight.color = Color.Lerp(profile.startColor, profile.endColor, t);
    }
}

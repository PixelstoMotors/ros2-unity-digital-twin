using UnityEngine;

public class DisablePhysics : MonoBehaviour
{
    void Start()
    {
        // Disable all ArticulationBodies
        ArticulationBody[] bodies = GetComponentsInChildren<ArticulationBody>();
        
        foreach (ArticulationBody ab in bodies)
        {
            ab.enabled = false;
            Debug.Log("Disabled ArticulationBody for: " + ab.name);
        }
        
        // Now rotate link2 by 30 degrees
        Transform link2 = FindDeepChild(transform, "link2");
        if (link2 != null)
        {
            link2.rotation = Quaternion.Euler(0, 30, 0) * link2.rotation;
            Debug.Log("Rotated link2 by 30 degrees!");
        }
        
        Debug.Log("Physics disabled - using Transform only!");
    }
    
    Transform FindDeepChild(Transform aParent, string aName)
    {
        foreach(Transform child in aParent)
        {
            if(child.name == aName) return child;
            var result = FindDeepChild(child, aName);
            if (result != null) return result;
        }
        return null;
    }
}

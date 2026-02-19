using UnityEngine;
using System.Collections.Generic;

public class AddArticulationBodies : MonoBehaviour
{
    void Start()
    {
        // Find all child objects with "link" in the name
        Transform[] allChildren = GetComponentsInChildren<Transform>();
        
        foreach (Transform child in allChildren)
        {
            string name = child.name.ToLower();
            
            if (name.Contains("base"))
            {
                // Base - make it immovable
                ArticulationBody ab = child.gameObject.AddComponent<ArticulationBody>();
                ab.immovable = true;
                Debug.Log("Added ArticulationBody (immovable) to: " + child.name);
            }
            else if (name.Contains("link"))
            {
                // Links - add articulation body
                if (child.gameObject.GetComponent<ArticulationBody>() == null)
                {
                    ArticulationBody ab = child.gameObject.AddComponent<ArticulationBody>();
                    ab.anchorRotation = Quaternion.identity;
                    Debug.Log("Added ArticulationBody to: " + child.name);
                }
            }
        }
        
        Debug.Log("ArticulationBody setup complete!");
    }
}

using UnityEngine;

public class SmoothJointMotion : MonoBehaviour
{
    void Start()
    {
        // Find all ArticulationBodies in children
        ArticulationBody[] bodies = GetComponentsInChildren<ArticulationBody>();
        
        foreach (ArticulationBody ab in bodies)
        {
            string name = ab.name.ToLower();
            
            // Skip base - keep it immovable
            if (name.Contains("base"))
                continue;
            
            // Skip unnamed/collision objects
            if (name.Contains("unnamed") || name.Contains("collision"))
                continue;
            
            // Set X Drive properties for smooth motion
            ArticulationDrive drive = ab.xDrive;
            drive.stiffness = 500f;
            drive.damping = 50f;
            drive.forceLimit = 50f;
            ab.xDrive = drive;
            
            // Increase damping to kill vibration
            ab.linearDamping = 20f;
            ab.angularDamping = 20f;
            
            // Set joint friction
            ab.jointFriction = 0.5f;
            
            Debug.Log("Smooth motion configured for: " + ab.name);
        }
        
        Debug.Log("Smooth joint motion setup complete for " + bodies.Length + " bodies!");
    }
}

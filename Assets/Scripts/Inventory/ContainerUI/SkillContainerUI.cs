using UnityEngine;

public class SkillContainerUI : ContainerUI
{
    private SkillContainer skillContainer;
    public GameObject playerObject; // or get via singleton

    protected override void Start()
    {
        base.Start();
        skillContainer = linkedContainer as SkillContainer;
    }

    void Update()
    {
        if (skillContainer == null) return;
        if (Input.GetKeyDown(KeyCode.Alpha1)) skillContainer.Use(0, playerObject);
        if (Input.GetKeyDown(KeyCode.Alpha2)) skillContainer.Use(1, playerObject);
        if (Input.GetKeyDown(KeyCode.Alpha3)) skillContainer.Use(2, playerObject);
        if (Input.GetKeyDown(KeyCode.Alpha4)) skillContainer.Use(3, playerObject);
    }
}
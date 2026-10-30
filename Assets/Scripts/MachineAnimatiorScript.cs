using UnityEngine;

public class MachineAnimatiorScript : MonoBehaviour
{
    private Animator anim;
    void Start()
    {
        anim = GetComponent<Animator>();
    }

    public void PlayAnim()
    {
        anim.SetInteger("SpinIndex", Random.Range(0, 3));
        anim.SetTrigger("Spin");
    }

}

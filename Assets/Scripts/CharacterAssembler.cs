using System.Collections.Generic;
using UnityEngine;

public class CharacterAssembler : MonoBehaviour
{
    public List<GameObject> torsos;
    public List<GameObject> heads;
    public List<GameObject> rightArms;
    public List<GameObject> leftArms;
    public List<GameObject> rightLegs;
    public List<GameObject> leftLegs;

    private void Start()
    {
        GameObject torso = Instantiate(torsos[Random.Range(0, torsos.Count)]);
        BodyPartAttacher bpa = torso.GetComponent<BodyPartAttacher>();

        bpa.headObject = Instantiate(heads[Random.Range(0, heads.Count)], bpa.headAttachPoint);
        bpa.leftArmObject = Instantiate(leftArms[Random.Range(0, leftArms.Count)], bpa.leftArmAttachPoint);
        bpa.rightArmObject = Instantiate(rightArms[Random.Range(0, rightArms.Count)], bpa.rightArmAttachPoint);
        bpa.leftLegObject = Instantiate(leftLegs[Random.Range(0, leftLegs.Count)], bpa.leftLegAttachPoint);
        bpa.rightLegObject = Instantiate(rightLegs[Random.Range(0, rightLegs.Count)], bpa.rightLegAttachPoint);
    }
}
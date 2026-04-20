using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CharacterAssembler : MonoBehaviour
{
    public List<GameObject> torsos;
    public List<GameObject> heads;
    public List<GameObject> rightArms;
    public List<GameObject> leftArms;
    public List<GameObject> rightLegs;
    public List<GameObject> leftLegs;
    
    private GameObject selectedTorso;
    private GameObject selectedHead;
    private GameObject selectedLeftArm;
    private GameObject selectedRightArm;
    private GameObject selectedLeftLeg;
    private GameObject selectedRightLeg;

    private void Start()
    {
        int randomTorsoIndex = Random.Range(0, torsos.Count);
        int randomHeadIndex = Random.Range(0, heads.Count);
        int randomLeftArmIndex = Random.Range(0, leftArms.Count);
        int randomRightArmIndex = Random.Range(0, rightArms.Count);
        int randomLeftLegIndex = Random.Range(0, leftLegs.Count);
        int randomRightLegIndex = Random.Range(0, rightLegs.Count);
        
        selectedTorso = torsos[randomTorsoIndex];
        selectedHead = heads[randomHeadIndex];
        selectedLeftArm = leftArms[randomLeftArmIndex];
        selectedRightArm = rightArms[randomRightArmIndex];
        selectedLeftLeg = leftLegs[randomLeftLegIndex];
        selectedRightLeg = rightLegs[randomRightLegIndex];
        

        GameObject torso = Instantiate(torsos.ElementAt(randomTorsoIndex));

        BodyPartAttacher bpa = torso.GetComponent<BodyPartAttacher>();
        bpa.headObject = Instantiate(heads.ElementAt(randomHeadIndex), bpa.headAttachPoint);
        
        BodyPartAttacher bpat = torso.GetComponent<BodyPartAttacher>();
        bpat.headObject = Instantiate(heads.ElementAt(randomHeadIndex), bpa.headAttachPoint);
    }
}
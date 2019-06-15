﻿using Cinemachine;
using UnityEngine;
using UnityEngine.UI;

// 주어진 Gun 오브젝트를 쏘거나 재장전
// 알맞은 애니메이션을 재생하고 IK를 사용해 캐릭터 양손이 총에 위치하도록 조정
public class PlayerShooter : MonoBehaviour
{
    public enum AimState { Idle, HipFire}

    private Camera playerCamera;
    public AimState aimState { get; private set; }

    public LayerMask excludeTarget;
    
    public Gun gun; // 사용할 총
    public Transform leftHandMount; // 총의 왼쪽 손잡이, 왼손이 위치할 지점
    
    private Animator playerAnimator; // 애니메이터 컴포넌트

    private Vector3 aimPoint;

    private PlayerInput playerInput;
    public float AngleYBetweenPlayerAndCamera => playerCamera.transform.eulerAngles.y - transform.eulerAngles.y;

    private bool linedUp => !(Mathf.Abs(AngleYBetweenPlayerAndCamera) > 1f);

    private bool hasEnoughDistance => !Physics.Linecast(transform.position + Vector3.up * 1.5f,gun.fireTransform.position,~excludeTarget);

    
    private void Start()
    {
        if (excludeTarget != (excludeTarget | (1 << gameObject.layer)))
        {
            excludeTarget |= (1 << gameObject.layer);
        }

        playerInput = GetComponent<PlayerInput>();
        playerCamera = Camera.main;
        // 사용할 컴포넌트들을 가져오기
        playerAnimator = GetComponent<Animator>();
    }

    private void OnEnable() {
        // 슈터가 활성화될 때 총도 함께 활성화
        
        gun.gameObject.SetActive(true);
        gun.Setup(this);
    }

    private void OnDisable() {
        // 슈터가 비활성화될 때 총도 함께 비활성화
        gun.gameObject.SetActive(false);
    }
    
    private void FixedUpdate() {
        
        if (playerInput.fire)
        {
            Shoot();
        }
        else if (playerInput.reload)
        {
            Reload();
        }
    }

    void Update()
    {
        UpdateAimTarget();
        
        var angle = playerCamera.transform.eulerAngles.x;
        if (angle > 90f) angle -= 360f;
        playerAnimator.SetFloat("Angle", angle / 90f);

        UpdateUI();
    }

    public void Shoot()
    {
        if (aimState == AimState.Idle)
        {
            if (linedUp)
            {
                aimState = AimState.HipFire;
            }
        }
        else if (aimState == AimState.HipFire)
        {
            if (hasEnoughDistance)
            {
                if(gun.Fire(aimPoint))
                {
                    playerAnimator.SetTrigger("Shoot");
                }
            }
            else
            {
                aimState = AimState.Idle;
            }
        }
    }

    public void Reload()
    {
        // 재장전 입력 감지시 재장전
        if (gun.Reload())
        {
            // 재장전 성공시에만 재장전 애니메이션 재생
            playerAnimator.SetTrigger("Reload");
        }
    }

    private void UpdateAimTarget()
    {
        RaycastHit hit;
        Vector3 lookPoint;
        
        var ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 1f));
        
        if (Physics.Raycast(ray, out hit, gun.fireDistance, ~excludeTarget))
        {
            lookPoint = hit.point;
        }
        else
        {
            lookPoint = playerCamera.transform.position + playerCamera.transform.forward * gun.fireDistance;
        }

        if(Physics.Linecast(gun.fireTransform.position, lookPoint, out hit, ~excludeTarget))
        {
            aimPoint = hit.point;
        }
        else
        {
            aimPoint = lookPoint;
        }
        
    }

    // 탄약 UI 갱신
    private void UpdateUI()
    {
        if (gun != null && UIManager.instance != null)
        {
            // UI 매니저의 탄약 텍스트에 탄창의 탄약과 남은 전체 탄약을 표시
            UIManager.instance.UpdateAmmoText(gun.magAmmo, gun.ammoRemain);

            UIManager.instance.SetActiveCrosshair(hasEnoughDistance);
            UIManager.instance.UpdateCrossHairPosition(aimPoint);
        }
    }
    
    // 애니메이터의 IK 갱신
    private void OnAnimatorIK(int layerIndex) {

        if (gun.state == Gun.State.Reloading) return;

        // IK를 사용하여 왼손의 위치와 회전을 총의 오른쪽 손잡이에 맞춘다
        playerAnimator.SetIKPositionWeight(AvatarIKGoal.LeftHand, 1.0f);
        playerAnimator.SetIKRotationWeight(AvatarIKGoal.LeftHand, 1.0f);

        playerAnimator.SetIKPosition(AvatarIKGoal.LeftHand,
            leftHandMount.position);
        playerAnimator.SetIKRotation(AvatarIKGoal.LeftHand,
            leftHandMount.rotation);
    }
}
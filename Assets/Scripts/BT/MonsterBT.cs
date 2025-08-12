using System.Collections.Generic;
using UnityEngine;

public class MonsterBT : MonoBehaviour
{
    public Transform target;          // 추적/공격 대상
    public Transform selfTransform;   // 몬스터 자신의 Transform
    public AIBlackboard bb;           // AI 상태 공유 블랙보드

    private BTNode root;

    void Start()
    {
        // 블랙보드 초기화
        bb = new AIBlackboard { target = target };

        // 이동 서브트리 (회피 우선, 추적 다음)
        var moveSelector = new Selector(new List<BTNode>
        {
            new MoveEvadeAction(selfTransform, bb, 4f),
            new MoveChaseAction(selfTransform, bb, 3f)
        });

        // 공격 서브트리
        var attackNode = new AttackAction(selfTransform, bb, 1.5f, 1f);

        // 루트 트리: 이동과 공격을 병렬로 실행
        root = new Parallel(new List<BTNode> { moveSelector, attackNode });
    }

    void Update()
    {
        root.Tick();
    }
}

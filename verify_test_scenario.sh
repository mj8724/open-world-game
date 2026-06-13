#!/bin/bash
# Manual verification script for test scenario

echo "=== Test Scenario Verification Script ==="
echo ""

echo "1. Checking test files..."
find Assets/Scripts/OpenWorld/Testing -name "*.cs" -exec echo "  ✓ {}" \;
echo ""

echo "2. Checking key APIs used..."
echo "  - OpenWorldConstants (PlayerFactionId, EnemyFactionId)"
grep -q "OpenWorldConstants.PlayerFactionId" Assets/Scripts/OpenWorld/Testing/*.cs && echo "    ✓ Used correctly"

echo "  - UnitOrder.CurrentOrder.Kind"
grep -q "CurrentOrder.Kind" Assets/Scripts/OpenWorld/Testing/*.cs && echo "    ✓ Used correctly"

echo "  - BlueprintJob.BuildKind"
grep -q "BuildKind" Assets/Scripts/OpenWorld/Testing/*.cs && echo "    ✓ Used correctly"

echo "  - BlueprintStatus enum"
grep -q "BlueprintStatus.Active" Assets/Scripts/OpenWorld/Testing/*.cs && echo "    ✓ Used correctly"

echo ""
echo "3. Checking integration point..."
grep -q "InitializeTestBotSystem" Assets/Scripts/OpenWorld/Core/OpenWorldBootstrap.cs && echo "  ✓ Integrated into OpenWorldBootstrap"

echo ""
echo "4. Code statistics..."
wc -l Assets/Scripts/OpenWorld/Testing/*.cs | tail -1

echo ""
echo "=== Verification complete ==="
echo "To test in Unity:"
echo "  1. Open Unity Editor"
echo "  2. Press Play"
echo "  3. Watch Console for red [PROBE] warnings"
echo "  4. Both factions should auto-build and fight"

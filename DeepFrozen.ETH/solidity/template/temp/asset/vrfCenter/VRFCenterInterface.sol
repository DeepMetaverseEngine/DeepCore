// SPDX-License-Identifier: MIT
pragma solidity ^0.8.0;

import "../preclude/Preclude.sol";

import "../ownable/OwnableInterface.sol";
import "./VRFCenterEvent.sol";

interface VRFCenterInterface is OwnableInterface, VRFCenterEvent {

    function requestVrf(address who, bytes32 reason) external returns (bytes32 requestId);

    function verifyVrf(
        bytes32 requestId,
        uint256[2] memory publicKey,
        uint256[4] memory proof,
        uint256[2] memory uPoint,
        uint256[4] memory vComponents
    ) external returns (bytes32 randomness);

    function feedVrf(
        bytes32 requestId,
        uint256[2] memory publicKey,
        uint256[4] memory proof,
        uint256[2] memory uPoint,
        uint256[4] memory vComponents
    ) external;

    function vrfAlphaString(bytes32 requestId) view external returns (bool);

    function vrfBetaString(bytes32 requestId) view external returns (bytes32);

    function setVRFProvider(uint256[2] memory publicKey, bool flag) external;

    function isVRFProvider(uint256[2] memory publicKey) view external returns (bool);
}

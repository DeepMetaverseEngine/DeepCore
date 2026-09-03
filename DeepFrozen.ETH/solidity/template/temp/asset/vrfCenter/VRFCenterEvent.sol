// SPDX-License-Identifier: MIT
pragma solidity ^0.8.0;

interface VRFCenterEvent {

    event RequestVRF(bytes32 indexed requestId);

    event VerifyVRF(bytes32 indexed requestId, bytes32 randomness);
}

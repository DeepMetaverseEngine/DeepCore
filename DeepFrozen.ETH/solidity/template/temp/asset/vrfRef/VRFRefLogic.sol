// SPDX-License-Identifier: MIT
pragma solidity ^0.8.0;

import "./VRFRefLayout.sol";
import "../vrfReceiver/VRFReceiverLogic.sol";
import "./VRFRefInterface.sol";

import "../vrfCenter/VRFCenterInterface.sol";

contract VRFRefLogic is VRFRefLayout, VRFReceiverLogic, VRFRefInterface {

    //do remember to lock 'something' first
    function requestVrf(address who, bytes32 reason) internal returns (bytes32 requestId){
        requestId = VRFCenterInterface(vrfCenter()).requestVrf(who, reason);
        return requestId;
    }

    function vrfContinue(
        bytes32 requestId,
        uint256[2] memory publicKey,
        uint256[4] memory proof,
        uint256[2] memory uPoint,
        uint256[4] memory vComponents
    ) internal returns (bytes32 randomness){
        randomness = VRFCenterInterface(vrfCenter()).verifyVrf(requestId, publicKey, proof, uPoint, vComponents);
        return randomness;
    }

    function _fulfillRandomness(bytes32 requestId, bytes32 randomness) virtual override internal {

    }
}

// SPDX-License-Identifier: MIT
pragma solidity ^0.8.0;

import "../preclude/Preclude.sol";

import "../nameServiceRef/NameServiceRefInterface.sol";
import "./VRFReceiverEvent.sol";

interface VRFReceiverInterface is NameServiceRefInterface, VRFReceiverEvent {

    function rawFulfillRandomness(bytes32 requestId, bytes32 randomness) external;

}

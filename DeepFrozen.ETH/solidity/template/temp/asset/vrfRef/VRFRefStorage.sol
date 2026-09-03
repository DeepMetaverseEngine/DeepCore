// SPDX-License-Identifier: MIT
pragma solidity ^0.8.0;

import "./VRFRefLayout.sol";
import "../vrfReceiver/VRFReceiverStorage.sol";

//this is a combining module, can be combined with others
abstract contract VRFRefStorage is VRFRefLayout, VRFReceiverStorage {

    constructor (
    )//    VRFReceiverStorage(nameService_)
    {
    }
}

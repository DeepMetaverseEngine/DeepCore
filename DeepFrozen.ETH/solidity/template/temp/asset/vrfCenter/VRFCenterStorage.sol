// SPDX-License-Identifier: MIT
pragma solidity ^0.8.0;

import "./VRFCenterLayout.sol";
import "../ownable/OwnableStorage.sol";

//this is an endpoint module, only can be directly inherited all the way to the end
contract VRFCenterStorage is VRFCenterLayout, OwnableStorage {

    constructor (
        address owner_
    )
    OwnableStorage(owner_){
    }
}

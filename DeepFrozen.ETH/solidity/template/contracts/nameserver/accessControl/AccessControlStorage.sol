// SPDX-License-Identifier: MIT
pragma solidity ^0.8.0;

import "./AccessControlLayout.sol";
import "../../nameserver/ownable/OwnableStorage.sol";

//this is an endpoint module, only can be directly inherited all the way to the end
contract AccessControlStorage is AccessControlLayout, OwnableStorage {

    constructor (
        address owner_
    )
        //quite stable
    OwnableStorage(owner_)
    {

    }
}

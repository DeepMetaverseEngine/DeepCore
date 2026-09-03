// SPDX-License-Identifier: MIT
pragma solidity ^0.8.0;

import "./NameServiceLayout.sol";
import "../../nameserver/accessControl/AccessControlStorage.sol";

//this is an endpoint module, only can be directly inherited all the way to the end
//this module substitutes {AccessControlStorage}, must be combined with combining modules using {AccessControlStorage}
contract NameServiceStorage is NameServiceLayout, AccessControlStorage {

    constructor (
        address owner_
    )
    AccessControlStorage(owner_)
    {

    }
}

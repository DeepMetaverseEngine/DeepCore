// SPDX-License-Identifier: MIT
pragma solidity ^0.8.0;

import "./DeputyCenterLayout.sol";
import "../../nameserver/ownable/OwnableStorage.sol";
import "../../nameserver/nameServiceRef/NameServiceRefStorage.sol";

//this is an endpoint module, only can be directly inherited all the way to the end
contract DeputyCenterStorage is DeputyCenterLayout, OwnableStorage, NameServiceRefStorage {

    constructor (
        address nameService_,
        address owner_
    )
    OwnableStorage(owner_)
    NameServiceRefStorage(nameService_)
    {

    }
}

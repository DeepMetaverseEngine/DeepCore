// SPDX-License-Identifier: MIT
pragma solidity ^0.8.0;

import "./DeputyRefLayout.sol";
import "../../nameserver/nameServiceRef/NameServiceRefStorage.sol";

//this is a combining module, can be combined with others
abstract contract DeputyRefStorage is DeputyRefLayout, NameServiceRefStorage {
    constructor ()
        //    NameServiceRefStorage(nameService_)
    {
    }
}

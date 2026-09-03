// SPDX-License-Identifier: MIT
pragma solidity ^0.8.0;

import "./HERC20IMLayout.sol";
import "../../nameserver/ownable/OwnableStorage.sol";
import "../../nameserver/nameServiceRef/NameServiceRefStorage.sol";
import "../../nameserver/reentrancy/ReentrancyStorage.sol";
import "../erc20/HERC20Storage.sol";

import "./HERC20IMType.sol";

//this is an endpoint module, only can be directly inherited all the way to the end
abstract contract HERC20IMStorage is HERC20IMLayout, OwnableStorage, NameServiceRefStorage, ReentrancyStorage, HERC20Storage {

    constructor (
        HERC20IMType.constructParam memory param,
        address owner_
    )
    OwnableStorage(owner_)
        //    AccessControlRefStorage(accessControl_)
    ReentrancyStorage()
    HERC20Storage(param.name, param.symbol, param.cap)
    {
        _supportTransfer = param.supportTransfer;
        _supportMint = param.supportMint;
        _supportBurn = param.supportBurn;
    }
}

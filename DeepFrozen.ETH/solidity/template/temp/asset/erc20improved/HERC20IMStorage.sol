// SPDX-License-Identifier: MIT
pragma solidity ^0.8.0;

import "./HERC20IMLayout.sol";
import "../ownable/OwnableStorage.sol";
import "../accessControlRef/AccessControlRefStorage.sol";
import "../reentrancy/ReentrancyStorage.sol";
import "../erc20/HERC20Storage.sol";

//this is an endpoint module, only can be directly inherited all the way to the end
abstract contract HERC20IMStorage is HERC20IMLayout, OwnableStorage, AccessControlRefStorage, ReentrancyStorage, HERC20Storage {

    constructor (
        string memory name_,
        string memory symbol_,
        uint256 cap_,
        address owner_
    )
    OwnableStorage(owner_)
        //    AccessControlRefStorage(accessControl_)
    ReentrancyStorage()
    HERC20Storage(name_, symbol_, cap_)
    {
        _supportTransfer = false;
        _supportMint = false;
        _supportBurn = false;
    }
}

// SPDX-License-Identifier: MIT
pragma solidity ^0.8.0;

import "./HERC4610IMLayout.sol";
import "../ownable/OwnableStorage.sol";
import "../accessControlRef/AccessControlRefStorage.sol";
import "../reentrancy/ReentrancyStorage.sol";
import "../erc4610/HERC4610Storage.sol";

//this is an endpoint module, only can be directly inherited all the way to the end
abstract contract HERC4610IMStorage is HERC4610IMLayout, OwnableStorage, AccessControlRefStorage, ReentrancyStorage, HERC4610Storage {

    constructor (
        string memory name_,
        string memory symbol_,
        string memory baseURI_,
        address owner_
    )
    OwnableStorage(owner_)
        // AccessControlRefStorage(accessControl_)
    ReentrancyStorage()
    HERC4610Storage(name_, symbol_, baseURI_){
        _supportTransfer = false;
        _supportMint = false;
        _supportBurn = false;
    }
}

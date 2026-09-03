// SPDX-License-Identifier: MIT
pragma solidity ^0.8.0;

import "./HERC721IMLayout.sol";
import "../ownable/OwnableStorage.sol";
import "../accessControlRef/AccessControlRefStorage.sol";
import "../reentrancy/ReentrancyStorage.sol";
import "../erc721/HERC721Storage.sol";

//this is an endpoint module, only can be directly inherited all the way to the end
abstract contract HERC721IMStorage is HERC721IMLayout, OwnableStorage, AccessControlRefStorage, ReentrancyStorage, HERC721Storage {

    constructor (
        string memory name_,
        string memory symbol_,
        string memory baseURI_,
        address owner_
    )
    OwnableStorage(owner_)
        // AccessControlRefStorage(accessControl_)
    ReentrancyStorage()
    HERC721Storage(name_, symbol_, baseURI_){
        _supportTransfer = false;
        _supportMint = false;
        _supportBurn = false;
    }
}

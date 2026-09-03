// SPDX-License-Identifier: MIT
pragma solidity ^0.8.0;

import "./HERC1155IMLayout.sol";
import "../ownable/OwnableStorage.sol";
import "../accessControlRef/AccessControlRefStorage.sol";
import "../reentrancy/ReentrancyStorage.sol";
import "../erc1155/HERC1155Storage.sol";

//this is an endpoint module, only can be directly inherited all the way to the end
abstract contract HERC1155IMStorage is HERC1155IMLayout, OwnableStorage, AccessControlRefStorage, ReentrancyStorage, HERC1155Storage {

    constructor (
        string memory uri_,
        bool supportTransfer_,
        bool supportMint_,
        bool supportBurn_,
        address owner_
    )
    OwnableStorage (owner_)
        //    AccessControlRefStorage(accessControl_)
    ReentrancyStorage()
    HERC1155Storage(uri_){
        _supportTransfer = supportTransfer_;
        _supportMint = supportMint_;
        _supportBurn = supportBurn_;
    }
}

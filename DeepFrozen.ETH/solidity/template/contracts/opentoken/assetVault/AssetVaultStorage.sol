// SPDX-License-Identifier: MIT
pragma solidity ^0.8.0;

import "./AssetVaultLayout.sol";
import "../../nameserver/nameServiceRef/NameServiceRefStorage.sol";

//this is a leaf module
abstract contract AssetVaultStorage is AssetVaultLayout, NameServiceRefStorage {

    constructor (){
    }
}

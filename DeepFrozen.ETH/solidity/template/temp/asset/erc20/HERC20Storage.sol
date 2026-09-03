// SPDX-License-Identifier: MIT
pragma solidity ^0.8.0;

import "./HERC20Layout.sol";
import "../context/ContextStorage.sol";

//this is an endpoint module, only can be directly inherited all the way to the end
contract HERC20Storage is HERC20Layout, ContextStorage {

    constructor (
        string memory name_,
        string memory symbol_,
        uint256 cap_
    )
    ContextStorage(){
        _name = name_;
        _symbol = symbol_;
        _decimals = 18;
        require(cap_ > 0, "ERC20Capped: cap is 0");
        _cap = cap_;
    }
}

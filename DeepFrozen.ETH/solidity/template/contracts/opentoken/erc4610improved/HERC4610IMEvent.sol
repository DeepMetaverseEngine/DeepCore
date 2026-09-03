// SPDX-License-Identifier: MIT
pragma solidity ^0.8.0;

interface HERC4610IMEvent {

    event Mint(address indexed to, uint256 indexed tokenId);

    event Burn(address indexed from, uint256 indexed tokenId);

    event Freeze(address indexed unlocker, uint256 indexed tokenId);

    event Thaw(address indexed unlocker, uint256 indexed tokenId);

}

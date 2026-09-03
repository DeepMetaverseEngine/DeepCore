// SPDX-License-Identifier: MIT
pragma solidity ^0.8.0;

import "../preclude/Preclude.sol";

import "../erc721/HERC721Layout.sol";

abstract contract HERC4610Layout is HERC721Layout {

    // Mapping from token ID to delegator address
    mapping(uint256 => address) internal _delegators;

    /*
     *     bytes4(keccak256('setDelegator(address,uint256)')) == 0xbd2f54d9
     *     bytes4(keccak256('delegatorOf(uint256)')) == 0x7bbfdc33
     *     bytes4(keccak256('safeTransferFrom(address,address,uint256,bool)')) == 0x9944b84b
     *
     *     => 0xbd2f54d9 ^ 0x7bbfdc33 ^ 0x9944b84b == 0x5FD430A1
     */
    bytes4 internal constant _INTERFACE_ID_ERC4610 = 0x5FD430A1;

    mapping(bytes32 => mapping(uint256 => uint256)) internal uint256Attribute;
    mapping(bytes32 => mapping(uint256 => bytes32)) internal bytes32Attribute;
    mapping(bytes32 => mapping(uint256 => address)) internal addressAttribute;
    mapping(bytes32 => mapping(uint256 => string)) internal stringAttribute;
}

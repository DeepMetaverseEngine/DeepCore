// SPDX-License-Identifier: MIT
pragma solidity ^0.8.0;

import "../../nameserver/preclude/Preclude.sol";

import "../../nameserver/accessControl/AccessControlLayout.sol";

import "./NameServiceType.sol";

abstract contract NameServiceLayout is AccessControlLayout {

    EnumerableSet.Bytes32Set internal _singleKeys;
    //key => address
    mapping(bytes32 => address) _singleRegistry;

    EnumerableSet.Bytes32Set internal _multipleKeys;
    //key => addresses
    mapping(bytes32 => EnumerableSet.AddressSet) _multipleRegistry;

}

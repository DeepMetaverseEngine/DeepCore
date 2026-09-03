// SPDX-License-Identifier: MIT
pragma solidity ^0.8.0;

import "../../nameserver/preclude/Preclude.sol";

import "../../nameserver/ownable/OwnableInterface.sol";
import "../../nameserver/nameServiceRef/NameServiceRefInterface.sol";
import "../../nameserver/reentrancy/ReentrancyInterface.sol";
import "../erc20/HERC20Interface.sol";
import './HERC20IMEvent.sol';

interface HERC20IMInterface is OwnableInterface, NameServiceRefInterface, ReentrancyInterface, HERC20Interface, HERC20IMEvent {

    function mintNormal(uint256 amount) external;

    function mintSudo(address to, uint256 amount) external;

    function burnNormal(uint256 amount) external;

    function burnSudo(address from, uint256 amount) external;

    function setAccessControl(address accessControl_) external;

    function setSupport(bool supportTransfer_, bool supportMint_, bool supportBurn_) external;

    function setBlockListFrom(address[] memory from, bool flag) external;

    function setBlockListTo(address[] memory to, bool flag) external;

    function setPrivilegeListFrom(address[] memory from, bool flag) external;

    function setPrivilegeListTo(address[] memory to, bool flag) external;
    //==================================

    function support() view external returns (bool supportTransfer, bool supportMint, bool supportBurn);

    function transferTxs() view external returns (uint256);

    function transferAmounts() view external returns (uint256);

    function interactAccountsLength() view external returns (uint256);

    function interactAccountsContains(address who) view external returns (bool);

    function interactedAccountsAt(uint256 index) view external returns (address);
}

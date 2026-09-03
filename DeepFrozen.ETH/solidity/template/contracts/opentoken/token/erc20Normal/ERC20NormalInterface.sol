// SPDX-License-Identifier: MIT
pragma solidity ^0.8.0;

interface ERC20NormalInterface {

    event OwnershipTransferred(address oldManger, address newManger);

    function mint(address account, uint256 amount) external;

    function burn(address account, uint256 amount) external;

    function managers() view external returns (address[] memory);

    function setManager(address[] memory who, bool[] memory add) external;

    function transferOwnership(address newOwner) external;
}
